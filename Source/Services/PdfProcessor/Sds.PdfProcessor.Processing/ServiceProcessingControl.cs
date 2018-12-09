using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CQRSlite.Bus;
using CQRSlite.Commands;
using CQRSlite.Config;
using CQRSlite.Events;
using Microsoft.Practices.ServiceLocation;
using Sds.CqrsLite;
using Sds.CqrsLite.Bus;
using Sds.Reflection;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.GridFs;
using Serilog;
using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Topshelf;

namespace Sds.PdfProcessor.Processing
{
    public class ServiceProcessingControl : ServiceControl
    {
        public static string Name { get { return Assembly.GetEntryAssembly().GetName().Name; } }
        public static string Title { get { return Assembly.GetEntryAssembly().GetTitle(); } }
        public static string Description { get { return Assembly.GetEntryAssembly().GetDescription(); } }
        public static string Version { get { return Assembly.GetEntryAssembly().GetVersion(); } }

        public ServiceProcessingControl()
        { }

        public bool Start(HostControl hostControl)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            Log.Information($"Service {Name} v.{Version} starting...");

            Log.Information($"Name: {Name}");
            Log.Information($"Title: {Title}");
            Log.Information($"Description: {Description}");
            Log.Information($"Version: {Version}");

            var builder = new ContainerBuilder();

            builder.Register(c => new GridFsStorage(Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["mongodb:connection"]), ConfigurationManager.AppSettings["mongodb:database-name"])).As<IBlobStorage>().SingleInstance();

            builder.RegisterType<ConsumeEventObserver>().As<IConsumeEventObserver>();
            builder.RegisterType<ConsumeCommandObserver>().As<IConsumeCommandObserver>();

            builder.Register(c => new RabbitMqBus(
                Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["RabbitMQ:ConnectionString"]),
                c.Resolve<IConsumeEventObserver>(),
                c.Resolve<IConsumeCommandObserver>()
                ))
                .As<ICommandSender>()
                .As<IEventPublisher>()
                .As<IHandlerRegistrar>()
                .SingleInstance();

            builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies()).Where(t =>
            {
                var interfaces = t.GetInterfaces();

                return
                    interfaces.Any(y => y.GetTypeInfo().IsGenericType && y.GetTypeInfo().GetGenericTypeDefinition() == typeof(ICancellableCommandHandler<>)) ||
                    interfaces.Any(y => y.GetTypeInfo().IsGenericType && y.GetTypeInfo().GetGenericTypeDefinition() == typeof(ICommandHandler<>)) ||
                    interfaces.Any(y => y.GetTypeInfo().IsGenericType && y.GetTypeInfo().GetGenericTypeDefinition() == typeof(ICancellableEventHandler<>)) ||
                    interfaces.Any(y => y.GetTypeInfo().IsGenericType && y.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEventHandler<>));
            });

            builder.RegisterType<ServiceProcessingControl>();

            var container = builder.Build();

            ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(container));

            var registrar = new BusRegistrar(new CqrsLiteDependencyResolver(container));
            registrar.Register(Assembly.GetEntryAssembly());

            Log.Information($"Service {Name} v.{Version} started");

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Log.Information("Stopping Sds.PdfProcessor.Processing Service...");

            return true;
        }
    }
}



