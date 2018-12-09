using Collector.Serilog.Enrichers.Assembly;
using CQRSlite.Domain.Exception;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.Observers;
using Sds.MassTransit.Settings;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Reflection;
using Sds.Serilog;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;

namespace Sds.Osdr.Persistence
{
    public class PersistenceService : IMicroService
    {
        public static string Name { get { return Assembly.GetEntryAssembly().GetName().Name; } }
        public static string Title { get { return Assembly.GetEntryAssembly().GetTitle(); } }
        public static string Description { get { return Assembly.GetEntryAssembly().GetDescription(); } }
        public static string Version { get { return Assembly.GetEntryAssembly().GetVersion(); } }

        public static IConfigurationRoot Configuration { get; set; }
        private IServiceProvider Container { get; set; }
        
        public void Start()
        {
            var builder = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json")
                 .AddEnvironmentVariables();

            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceSystemInformationalVersionEnricher<PersistenceService>>()
                .ReadFrom.Configuration(Configuration)
                .MinimumLevel.ControlledBy(new EnvironmentVariableLoggingLevelSwitch("%OSDR_LOG_LEVEL%"))
                .CreateLogger();

            Log.Information($"Service {Name} v.{Version} starting...");

            Log.Information($"Name: {Name}");
            Log.Information($"Title: {Title}");
            Log.Information($"Description: {Description}");
            Log.Information($"Version: {Version}");

            var services = new ServiceCollection();

            services.AddOptions();
            services.Configure<MassTransitSettings>(Configuration.GetSection("MassTransit"));

            services.AddSingleton(new MongoClient(Environment.ExpandEnvironmentVariables(Configuration["ConnectionSettings:ConnectionString"])));
            services.AddScoped(service => service.GetService<MongoClient>().GetDatabase(Configuration["ConnectionSettings:DatabaseName"]));

            services.AddAllConsumers();

            var assemblies = new Assembly[]
            {
                Assembly.LoadFrom("Sds.Osdr.Generic.dll"),
                Assembly.LoadFrom("Sds.Osdr.RecordsFile.dll"),
                Assembly.LoadFrom("Sds.Osdr.Chemicals.dll"),
                Assembly.LoadFrom("Sds.Osdr.Crystals.dll"),
                Assembly.LoadFrom("Sds.Osdr.Reactions.dll"),
                Assembly.LoadFrom("Sds.Osdr.Spectra.dll"),
                Assembly.LoadFrom("Sds.Osdr.Pdf.dll"),
                Assembly.LoadFrom("Sds.Osdr.Images.dll"),
                Assembly.LoadFrom("Sds.Osdr.Office.dll"),
                Assembly.LoadFrom("Sds.Osdr.Tabular.dll"),
                Assembly.LoadFrom("Sds.Osdr.MachineLearning.dll"),
                Assembly.LoadFrom("Sds.Osdr.WebPage.dll"),
            };

            Log.Information($"Registered modules:");
            foreach (var module in assemblies)
            {
                Log.Information($"Module: {module.FullName}");
            }

            services.UsePersistenceOsdrModules(assemblies);

            services.AddSingleton(context => Bus.Factory.CreateUsingRabbitMq(x =>
            {
                var mtSettings = Container.GetService<IOptions<MassTransitSettings>>().Value;

                IRabbitMqHost host = x.Host(new Uri(Environment.ExpandEnvironmentVariables(mtSettings.ConnectionString)), h => { });

                x.UseDelayedExchangeMessageScheduler();

                x.UseSerilog();

                x.RegisterPersistenceOsdrModules(host, context, e =>
                {
                    e.UseDelayedRedelivery(r =>
                    {
                        r.Interval(mtSettings.RedeliveryCount, TimeSpan.FromMilliseconds(mtSettings.RedeliveryInterval));
                        r.Handle<ConcurrencyException>();
                    });
                    e.UseMessageRetry(r =>
                    {
                        r.Interval(mtSettings.RetryCount, TimeSpan.FromMilliseconds(mtSettings.RetryInterval));
                        r.Handle<ConcurrencyException>();
                    });

                    e.PrefetchCount = mtSettings.PrefetchCount;
                    e.UseInMemoryOutbox();
                }, assemblies);

                x.UseConcurrencyLimit(mtSettings.ConcurrencyLimit);

                JsonConvert.DefaultSettings = (() =>
                {
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new StringEnumConverter());
                    return settings;
                });
            }));

            Container = services.BuildServiceProvider();

            var busControl = Container.GetRequiredService<IBusControl>();

            busControl.ConnectPublishObserver(new PublishObserver());
            busControl.ConnectConsumeObserver(new ConsumeObserver());

            busControl.Start();

            if (int.TryParse(Configuration["HeartBeat:TcpPort"], out int port))
            {
                Heartbeat.TcpPortListener.Start(port);
            }
            
            // create indexes for mongodb
            var database = Container.GetService<IMongoDatabase>();
            
            Configuration
                .GetSection("OsdrConnectionSettings")
                .GetSection("Indexes")
                .GetChildren()
                .Select(x => new
                {
                    Name = x.GetSection("Name").Value,
                    Fields = x.GetSection("Fields").GetChildren().Select(f => f.Value)
                })
                .ToList()
                .ForEach(collectionInfo =>
                {
                    var collectionName = collectionInfo.Name;
                    var collectionIndexes = collectionInfo.Fields;

                    var mongoCollection = database.GetCollection<BsonDocument>(collectionName);
                    
                    foreach (var field in collectionIndexes)
                    {
                        mongoCollection.Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys
                            .Ascending(_ => _[field])).GetAwaiter().GetResult();
                    }
                    
                    Console.Write("");
                });

            Log.Information($"Service {Name} v.{Version} started");
        }

        public void Stop()
        {
            var busControl = Container.GetRequiredService<IBusControl>();
            busControl.Stop();
        }
    }
}
