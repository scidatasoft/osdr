using Collector.Serilog.Enrichers.Assembly;
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
using Sds.Serilog;
using Serilog;
using System;
using System.Reflection;

namespace Sds.MetadataStorage.Processing
{
    public class ProcessingService : IMicroService
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
                 .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceSystemInformationalVersionEnricher<ProcessingService>>()
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
            services.AddSingleton(new MongoClient(Environment.ExpandEnvironmentVariables(Configuration["OsdrConnectionSettings:ConnectionString"])));
            services.AddScoped(service => service.GetService<MongoClient>().GetDatabase(Configuration["OsdrConnectionSettings:DatabaseName"]));

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
                    r.Handle<MongoException>();
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
