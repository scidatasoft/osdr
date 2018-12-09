using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PeterKottas.DotNetCore.WindowsService.Base;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Extensions;
using Sds.Reflection;
using Serilog;
using System;
using System.Reflection;

namespace Sds.ChemicalProperties.Persistence
{
    public class PersistenceService : MicroService, IMicroService
    {
        public static string Name { get { return Assembly.GetEntryAssembly().GetName().Name; } }
        public static string Title { get { return Assembly.GetEntryAssembly().GetTitle(); } }
        public static string Description { get { return Assembly.GetEntryAssembly().GetDescription(); } }
        public static string Version { get { return Assembly.GetEntryAssembly().GetVersion(); } }

        public static IConfigurationRoot Configuration { get; set; }
        public static IBusControl BusControl { get; set; }

        public void Start()
        {
            var builder = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            Log.Information($"Service {Name} v.{Version} starting...");

            Log.Information($"Name: {Name}");
            Log.Information($"Title: {Title}");
            Log.Information($"Description: {Description}");
            Log.Information($"Version: {Version}");

            var services = new ServiceCollection();

            var connectionString = Environment.ExpandEnvironmentVariables(Configuration["OsdrConnectionSettings:ConnectionString"]);
            services.AddSingleton(new MongoClient(connectionString));
            services.AddScoped(service => service.GetService<MongoClient>().GetDatabase(Configuration["OsdrConnectionSettings:DatabaseName"]));

            services.AddSingleton(Configuration);
            services.AddAllConsumers();

            services.AddSingleton(context => Bus.Factory.CreateUsingRabbitMq(x =>
            {
                IRabbitMqHost host = x.Host(new Uri(Environment.ExpandEnvironmentVariables(Configuration["RabbitMQ:ConnectionString"])), h => { });

                x.UseSerilog();

                x.RegisterConsumers(host, context, e =>
                {
                    e.PrefetchCount = 8;
                    e.UseInMemoryOutbox();
                });

                x.UseRetry(r =>
                {
                    r.Incremental(100, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
                    r.Handle<MongoWriteException>();
                });
            }));

            var serviceProvider = services.BuildServiceProvider();

            BusControl = serviceProvider.GetRequiredService<IBusControl>();

            BusControl.Start();

            Log.Information($"Service {Name} v.{Version} started");

            StartBase();
        }

        public void Stop()
        {
            BusControl.Stop();

            StopBase();
        }
    }
}
