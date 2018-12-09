using Collector.Serilog.Enrichers.Assembly;
using CQRSlite.Domain;
using CQRSlite.Domain.Exception;
using CQRSlite.Events;
using GreenPipes;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.RabbitMqTransport;
using MassTransit.Scoping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PeterKottas.DotNetCore.WindowsService.Base;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using Sds.CqrsLite.EventStore;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.Observers;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Settings;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Reflection;
using Sds.Serilog;
using Serilog;
using System;
using System.Reflection;

namespace Sds.Osdr.Domain.BackEnd
{
    public class DomainBackEndService : MicroService, IMicroService
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

            Configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", false, true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceSystemInformationalVersionEnricher<DomainBackEndService>>()
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

            services.AddSingleton<IEventStore>(y => new GetEventStore(Environment.ExpandEnvironmentVariables(Configuration["EventStore:ConnectionString"])));
            services.AddSingleton<IEventPublisher, CqrsLite.MassTransit.MassTransitBus>();
            services.AddTransient<ISession, Session>();
            services.AddSingleton<IRepository, Repository>();

            services.AddSingleton<IConsumerScopeProvider, DependencyInjectionConsumerScopeProvider>();

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
                Assembly.LoadFrom("Sds.Osdr.WebPage.dll")
            };

            Log.Information($"Registered modules:");
            foreach (var module in assemblies)
            {
                Log.Information($"Module: {module.FullName}");
            }

            services.UseBackEndOsdrModules(assemblies);

            services.AddSingleton(container => Bus.Factory.CreateUsingRabbitMq(x =>
            {
                var mtSettings = Container.GetService<IOptions<MassTransitSettings>>().Value;

                IRabbitMqHost host = x.Host(new Uri(Environment.ExpandEnvironmentVariables(mtSettings.ConnectionString)), h => { });

                x.UseSerilog();

                x.RegisterConsumers(host, container, e =>
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
                });

                x.RegisterBackEndOsdrModules(host, container, e =>
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
                settings.Converters.Add(new StringEnumConverter());
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
