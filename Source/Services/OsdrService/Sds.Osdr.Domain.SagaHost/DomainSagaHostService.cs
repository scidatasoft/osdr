using Automatonymous;
using Collector.Serilog.Enrichers.Assembly;
using CQRSlite.Domain.Exception;
using GreenPipes;
using MassTransit;
using MassTransit.MongoDbIntegration;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PeterKottas.DotNetCore.WindowsService.Base;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using Sds.MassTransit.MongoDb.Saga;
using Sds.MassTransit.Observers;
using Sds.MassTransit.Saga;
using Sds.MassTransit.Settings;
using Sds.Osdr.Domain.AccessControl;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Reflection;
using Sds.Serilog;
using Serilog;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Sds.Osdr.Domain.SagaHost
{
    public class DomainSagaHostService : MicroService, IMicroService
    {
        public static string Name { get { return Assembly.GetEntryAssembly().GetName().Name; } }
        public static string Title { get { return Assembly.GetEntryAssembly().GetTitle(); } }
        public static string Description { get { return Assembly.GetEntryAssembly().GetDescription(); } }
        public static string Version { get { return Assembly.GetEntryAssembly().GetVersion(); } }

        public static IConfigurationRoot Configuration { get; set; }
        private IServiceProvider Container { get; set; }

        public void Start()
        {
            StartBase();

            var builder = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", true, true)
                 .AddEnvironmentVariables();

            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceSystemInformationalVersionEnricher<DomainSagaHostService>>()
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
            services.Configure<Infrastructure.AccessControl.AccessControl>(Configuration.GetSection("AccessControl"));
            services.AddSingleton<IAccessControl, Infrastructure.AccessControl.AppSettingsAccessControl>();

            services.AddSingleton<ISagaRepositoryFactory>(new MongoDbSagaRepositoryFactory(Environment.ExpandEnvironmentVariables(Configuration["MongoDb:ConnectionString"])));
            
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

            services.UseSagaHostOsdrModules(assemblies);

            services.AddSingleton(context => Bus.Factory.CreateUsingRabbitMq(x =>
            {
                var mtSettings = Container.GetService<IOptions<MassTransitSettings>>().Value;

                IRabbitMqHost host = x.Host(new Uri(Environment.ExpandEnvironmentVariables(mtSettings.ConnectionString)), h => { });

                x.ConfigureJsonSerializer(settings => 
                {
                    settings.DefaultValueHandling = DefaultValueHandling.Include;
                    return settings;
                });

                x.UseInMemoryOutbox();

                x.UseSerilog();

                x.UseDelayedExchangeMessageScheduler();

                x.RegisterSagaHostOsdrModules(host, context, e =>
                {
                    e.UseDelayedRedelivery(r =>
                    {
                        r.Interval(mtSettings.RedeliveryCount, TimeSpan.FromMilliseconds(mtSettings.RedeliveryInterval));
                        r.Handle<MongoDbConcurrencyException>();
                        r.Handle<UnhandledEventException>();
                        r.Handle<ConcurrencyException>();
                    });
                    e.UseMessageRetry(r =>
                    {
                        r.Interval(mtSettings.RetryCount, TimeSpan.FromMilliseconds(mtSettings.RetryInterval));
                        r.Handle<MongoDbConcurrencyException>();
                        r.Handle<UnhandledEventException>();
                        r.Handle<ConcurrencyException>();
                    });

                    e.PrefetchCount = mtSettings.PrefetchCount;
                    e.UseInMemoryOutbox();
                }, assemblies);

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

            JsonConvert.DefaultSettings = (() =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter { CamelCaseText = false });
                return settings;
            });

            Log.Information($"Service {Name} v.{Version} started");
        }

        public void Stop()
        {
            var busControl = Container.GetRequiredService<IBusControl>();
            busControl.Stop();

            StopBase();
        }
    }
}
