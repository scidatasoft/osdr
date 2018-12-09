using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using GreenPipes;
using MassTransit;
using Sds.Cvsp;
using Sds.Cvsp.Compounds;
using Sds.MassTransit.AutofacIntegration;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.Observers;
using Sds.Reflection;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.GridFs;
using Serilog;
using System;
using System.Configuration;
using System.Reflection;
using Topshelf;

namespace Sds.ChemicalStandardizationValidation.Processing
{
    public class ServiceProcessingControl : ServiceControl
    {
        public static string Name { get { return Assembly.GetEntryAssembly().GetName().Name; } }
        public static string Title { get { return Assembly.GetEntryAssembly().GetTitle(); } }
        public static string Description { get { return Assembly.GetEntryAssembly().GetDescription(); } }
        public static string Version { get { return Assembly.GetEntryAssembly().GetVersion(); } }

        private IContainer Container { get; set; }

        public bool Start(HostControl hostControl)
        {
            Log.Information($"Service {Name} v.{Version} starting...");

            Log.Information($"Name: {Name}");
            Log.Information($"Title: {Title}");
            Log.Information($"Description: {Description}");
            Log.Information($"Version: {Version}");

            var builder = new ContainerBuilder();

            builder.Register(c => new GridFsStorage(
                Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["OsdrConnectionSettings:ConnectionString"]),
                ConfigurationManager.AppSettings["OsdrConnectionSettings:DatabaseName"]
                )).As<IBlobStorage>();

            builder.RegisterModule<Sds.Cvsp.Compounds.Autofac.ValidationStandardizationModule>();
            builder.Register(c => new XmlIssuesConfig(CompoundResources.LogCodes)).As<IIssuesConfig>();

            builder.RegisterConsumers(Assembly.GetExecutingAssembly());

            builder.Register(context =>
            {
                var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    var mtSettings = ConfigurationManager.AppSettings.MassTransitSettings();

                    var host = cfg.Host(new Uri(Environment.ExpandEnvironmentVariables(mtSettings.ConnectionString)), h => { });

                    cfg.RegisterConsumers(host, context, e =>
                    {
                        e.PrefetchCount = mtSettings.PrefetchCount;
                    });

                    cfg.UseConcurrencyLimit(mtSettings.ConcurrencyLimit);
                });

                return busControl;
            })
            .SingleInstance()
            .As<IBusControl>()
            .As<IBus>();

            Container = builder.Build();

            ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(Container));

            var bc = Container.Resolve<IBusControl>();

            bc.ConnectPublishObserver(new PublishObserver());
            bc.ConnectConsumeObserver(new ConsumeObserver());

            bc.Start();

            if (int.TryParse(ConfigurationManager.AppSettings["HeartBeat:TcpPort"], out int port))
            {
                Heartbeat.TcpPortListener.Start(port);
            }

            Log.Information($"Service {Name} v.{Version} started");

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Log.Information($"Stopping {Name} Service...");

            var bc = Container.Resolve<IBusControl>();
            bc.Stop();

            return true;
        }
    }
}
