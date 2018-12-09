using Autofac;
using MassTransit;
using Sds.MassTransit.AutofacIntegration;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.Observers;
using Sds.Reflection;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.GridFs;
using Sds.WebImporter.PdfProcessing.CommandHandlers;
using Serilog;
using System;
using System.Configuration;
using System.Reflection;
using Topshelf;

namespace Sds.WebImporter.PdfProcessing
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

            builder.Register(c => new GridFsStorage(Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["mongodb:connection"]), ConfigurationManager.AppSettings["mongodb:database-name"])).As<IBlobStorage>().SingleInstance();

            //ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(container));

            builder.RegisterConsumers(Assembly.GetExecutingAssembly());

            builder.Register(context =>
            {
                var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    var host = cfg.Host(new Uri(Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["RabbitMQ:ConnectionString"])), h => { });

                    cfg.RegisterConsumer<GeneratePdfFromHtmlCommandHandler>(host, context, e =>
                    {
                        e.PrefetchCount = 8;
                    });
                });

                return busControl;
            })
            .SingleInstance()
            .As<IBusControl>()
            .As<IBus>();

            Container = builder.Build();

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

            return true;
        }
    }
}



