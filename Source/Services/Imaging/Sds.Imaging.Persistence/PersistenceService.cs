using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
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
using System.Reflection;

namespace Sds.Imaging.Persistence
{
    public class PersistenceService : IMicroService
    {
        public static string Name { get { return Assembly.GetEntryAssembly().GetName().Name; } }
        public static string Title { get { return Assembly.GetEntryAssembly().GetTitle(); } }
        public static string Description { get { return Assembly.GetEntryAssembly().GetDescription(); } }
        public static string Version { get { return Assembly.GetEntryAssembly().GetVersion(); } }

        public static IConfigurationRoot Configuration { get; set; }
        public static IServiceProvider Container { get; set; }

        public void Start()
        {
            var builder = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            Log.Information($"Service {Name} v.{Version} starting...");

            Log.Information($"Name: {Name}");
            Log.Information($"Title: {Title}");
            Log.Information($"Description: {Description}");
            Log.Information($"Version: {Version}");

            var services = new ServiceCollection();

            services.AddOptions();
            services.Configure<MassTransitSettings>(Configuration.GetSection("MassTransit"));

            //Register MongoDB and GridFS 
            var connectionString = Environment.ExpandEnvironmentVariables(Configuration["OsdrConnectionSettings:ConnectionString"]);
            services.AddTransient(x => new MongoClient(connectionString).GetDatabase(Configuration["OsdrConnectionSettings:DatabaseName"]));
            services.AddTransient<IBlobStorage, GridFsStorage>(x => new GridFsStorage(x.GetService<IMongoDatabase>()));

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
                    r.Incremental(100, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
                    r.Handle<MongoWriteException>();
                });

                x.UseConcurrencyLimit(mtSettings.ConcurrencyLimit);
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

            Log.Information($"Service {Name} v.{Version} started");
        }

        public void Stop()
        {
            var busControl = Container.GetRequiredService<IBusControl>();
            busControl.Stop();
        }
    }
}
