using Collector.Serilog.Enrichers.Assembly;
using Elasticsearch.Net;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nest;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.Observers;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Settings;
using Sds.Reflection;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.GridFs;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sds.Osdr.Indexing
{
    public class IndexingService : IMicroService
    {
        public static string Name { get { return Assembly.GetEntryAssembly().GetName().Name; } }
        public static string Title { get { return Assembly.GetEntryAssembly().GetTitle(); } }
        public static string Description { get { return Assembly.GetEntryAssembly().GetDescription(); } }
        public static string Version { get { return Assembly.GetEntryAssembly().GetVersion(); } }

        public static IConfigurationRoot Configuration { get; set; }
        private IServiceProvider Container { get; set; }

        private async Task CreateMappings(IElasticClient elasticClient, IndexName indexName, TypeName typeName)
        {
            var result = await elasticClient.IndexExistsAsync(indexName);
            if (!result.Exists)
            {
                ICreateIndexRequest createIndexRequest;
                switch (indexName.Name)
                {
                    case "records":
                        createIndexRequest = new CreateIndexDescriptor(indexName)
                            .Mappings(s => s
                                .Map(typeName, tm => tm
                                    .Properties(p => p.Keyword(k => k.Name("OwnedBy")))
                                    .Properties(p => p.Keyword(k => k.Name("FileId")))
                                    .Properties(p => p.Object<dynamic>(f => f.Name("Properties")
                                        .Properties(np => np.Object<dynamic>(t => t.Name("ChemicalProperties")
                                            .Properties(cp => cp.Text(tp => tp.Name("Value")))))))));
                        break;

                    default:
                        createIndexRequest = new CreateIndexDescriptor(indexName)
                            .Mappings(s => s
                                .Map(typeName, tm => tm
                                    .Properties(p => p
                                        .Keyword(k => k.Name("OwnedBy"))
                                        )));
                        break;
                }

                try
                {
                    await elasticClient.CreateIndexAsync(createIndexRequest);
                    Log.Information($"Created mapping for index: '{indexName}', type: '{typeName}'");
                }
                catch (ElasticsearchClientException e)
                {
                    Log.Error($"Creating mapping server error: {e.Response.ServerError}");
                }
            }
        }

        private async Task SetupAnlyzer(IElasticClient elasticClient, IndexName indexName)
        {
            await elasticClient.CloseIndexAsync(indexName);

            await elasticClient.UpdateIndexSettingsAsync(
                new UpdateIndexSettingsRequest(indexName)
                {
                    IndexSettings = new IndexSettings
                    {
                        Analysis = new Analysis
                        {
                            Analyzers = new Analyzers
                            {
                                {
                                    "default", new CustomAnalyzer()
                                    {
                                        Filter = new [] { "lowercase"},
                                        Tokenizer = "whitespace"
                                    }
                                }
                            }
                        }
                    }
                });

            await elasticClient.OpenIndexAsync(indexName);
        }

        public void Start()
        {
            var builder = new ConfigurationBuilder()
                  .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceSystemInformationalVersionEnricher<IndexingService>>()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            Log.Information($"Service {Name} v.{Version} starting...");

            Log.Information($"Name: {Name}");
            Log.Information($"Title: {Title}");
            Log.Information($"Description: {Description}");
            Log.Information($"Version: {Version}");

            var services = new ServiceCollection();

            services.AddOptions();
            services.Configure<MassTransitSettings>(Configuration.GetSection("MassTransit"));

            string elasticConnectionString = Environment.ExpandEnvironmentVariables(Configuration["ElasticSearch:ConnectionString"]);
            var settings = new ConnectionSettings(new Uri(elasticConnectionString))
                .ThrowExceptions()
                .DefaultFieldNameInferrer(f => f); 
            services.AddSingleton<IElasticClient>(new ElasticClient(settings));
            
            var mongoDBConnectionString = Environment.ExpandEnvironmentVariables(Configuration["OsdrConnectionSettings:ConnectionString"]);
            services.AddSingleton(new MongoClient(mongoDBConnectionString));
            var database = Configuration["OsdrConnectionSettings:DatabaseName"];

            services.AddTransient<IBlobStorage, GridFsStorage>(
                x => new GridFsStorage(mongoDBConnectionString, Configuration["OsdrConnectionSettings:DatabaseName"]));

            Log.Information($"Using to MongoDB database {database}");
            services.AddScoped(service => service.GetService<MongoClient>().GetDatabase(database));

            services.AddAllConsumers();

            services.AddSingleton(context => Bus.Factory.CreateUsingRabbitMq(x =>
            {
                var mtSettings = Container.GetService<IOptions<MassTransitSettings>>().Value;
                IRabbitMqHost host = x.Host(new Uri(Environment.ExpandEnvironmentVariables(mtSettings.ConnectionString)), h => { });

                x.UseSerilog();

                x.RegisterConsumers(host, context, e =>
                {
                    e.PrefetchCount = mtSettings.PrefetchCount;
                    e.UseInMemoryOutbox();
                });

                x.UseRetry(r =>
                {
                    r.Incremental(mtSettings.RetryCount, TimeSpan.FromSeconds(mtSettings.RetryInterval), TimeSpan.FromMilliseconds(100));
                    r.Handle<CQRSlite.Domain.Exception.ConcurrencyException>();
                });

                x.UseConcurrencyLimit(mtSettings.ConcurrencyLimit);
            }));

            Container = services.BuildServiceProvider();

            var elasticClient = Container.GetRequiredService<IElasticClient>();

            try
            {
                new[]
                {
                    new { Index = "files", Type = "file" },
                    new { Index = "folders", Type = "folder" },
                    new { Index = "records", Type = "record" },
                    new { Index = "models", Type = "model" },
                }.ToList()
                .AsParallel()
                .ForAll(i =>
                    {
                        CreateMappings(elasticClient, i.Index, i.Type).Wait();
                        SetupAnlyzer(elasticClient, i.Index).Wait();
                    });

                elasticClient.PutPipeline("process_blob", p => p
                    .Description("Document attachment pipeline")
                    .Processors(pr => pr
                        .Attachment<object>(a => a.Field("Blob.Base64Content").TargetField("Blob.ParsedContent").IgnoreMissing())
                        .Remove<object>(r => r.Field("Blob.Base64Content"))
                        ));
            }
            catch (ElasticsearchClientException e)
            {
                Log.Error($"Creating pipeline server response: {e.Response.ServerError}");
            }

            var busControl = Container.GetRequiredService<IBusControl>();

            busControl.ConnectPublishObserver(new PublishObserver());
            busControl.ConnectConsumeObserver(new ConsumeObserver());

            busControl.Start();

            Log.Information($"Service {Name} v.{Version} started");

            if (int.TryParse(Configuration["HeartBeat:TcpPort"], out int port))
            {
                Heartbeat.TcpPortListener.Start(port);
            }
        }

        public void Stop()
        {
            var busControl = Container.GetRequiredService<IBusControl>();
            busControl.Stop();

            Log.Information($"Service {Name} v.{Version} stopped");
        }
    }
}
