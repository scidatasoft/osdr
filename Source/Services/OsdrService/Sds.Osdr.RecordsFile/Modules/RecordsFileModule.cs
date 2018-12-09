using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using Sds.CqrsLite.MassTransit.Filters;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.RabbitMq;
using Sds.Osdr.Domain.Modules;
using Sds.Storage.Blob.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.Modules
{
    public class RecordsFileModule : IModule
    {
        public RecordsFileModule()
        {
        }

        public bool IsSupported(BlobLoaded blob)
        {
            return false;
        }

        public Task Process(BlobLoaded blob)
        {
            throw new NotSupportedException();
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static void UseInMemoryModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, RecordsFileModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.Files.AddAggregatedPropertiesCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Files.AddFieldsCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Files.UpdateTotalRecordsCommandHandler>();

            services.AddScoped<BackEnd.CommandHandlers.Records.AddImageCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.AddIssuesCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.AddPropertiesCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.ChangeStatusCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.CreateInvalidRecordEventHandlers>();
            services.AddScoped<BackEnd.CommandHandlers.Records.DeleteCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.SetAccessPermissionsCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.UpdateFieldsCommandHandler>();

            services.AddScoped<BackEnd.EventHandlers.Nodes.RecordEventHandlers>();

            //  add persistence consumers...
            services.AddTransient<Persistence.CommandHandlers.AggregatePropertiesCommandHandler>();
            services.AddTransient<Persistence.EventHandlers.Files.FilesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Files.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Records.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Records.RecordsEventHandlers>();
        }

        public static void UseBackEndModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, RecordsFileModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.Files.AddAggregatedPropertiesCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Files.AddFieldsCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Files.UpdateTotalRecordsCommandHandler>();

            services.AddScoped<BackEnd.CommandHandlers.Records.AddImageCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.AddIssuesCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.AddPropertiesCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.ChangeStatusCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.CreateInvalidRecordEventHandlers>();
            services.AddScoped<BackEnd.CommandHandlers.Records.DeleteCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.SetAccessPermissionsCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Records.UpdateFieldsCommandHandler>();

            services.AddScoped<BackEnd.EventHandlers.Nodes.RecordEventHandlers>();
        }

        public static void UsePersistenceModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, RecordsFileModule>();

            //  add persistence consumers...
            services.AddTransient<Persistence.CommandHandlers.AggregatePropertiesCommandHandler>();
            services.AddTransient<Persistence.EventHandlers.Files.FilesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Files.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Records.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Records.RecordsEventHandlers>();
        }
    }

    public static class ConfigurationExtensions
    {
        public static void RegisterInMemoryModule(this IBusFactoryConfigurator configurator, IServiceProvider provider)
        {
            //  add backend consumers...
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.AddAggregatedPropertiesCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.AddFieldsCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.UpdateTotalRecordsCommandHandler>(provider, null, c => c.UseCqrsLite());

            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.AddImageCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.AddIssuesCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.AddPropertiesCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.ChangeStatusCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.CreateInvalidRecordEventHandlers>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.DeleteCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.SetAccessPermissionsCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.UpdateFieldsCommandHandler>(provider, null, c => c.UseCqrsLite());

            configurator.RegisterScopedConsumer<BackEnd.EventHandlers.Nodes.RecordEventHandlers>(provider, null, c => c.UseCqrsLite());

            //  add persistence consumers...
            configurator.RegisterConsumer<Persistence.CommandHandlers.AggregatePropertiesCommandHandler>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.Files.FilesEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.Files.NodesEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.Records.NodesEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.Records.RecordsEventHandlers>(provider);
        }

        public static void RegisterBackEndModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register backend consumers...
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.AddAggregatedPropertiesCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.AddFieldsCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.UpdateTotalRecordsCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());

            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.AddImageCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.AddIssuesCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.AddPropertiesCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.ChangeStatusCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.CreateInvalidRecordEventHandlers>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.DeleteCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.SetAccessPermissionsCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Records.UpdateFieldsCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());

            configurator.RegisterScopedConsumer<BackEnd.EventHandlers.Nodes.RecordEventHandlers>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
        }

        public static void RegisterPersistenceModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register persistence consumers...
            configurator.RegisterConsumer<Persistence.CommandHandlers.AggregatePropertiesCommandHandler>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.Files.FilesEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.Files.NodesEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.Records.NodesEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.Records.RecordsEventHandlers>(host, provider, endpointConfigurator);
        }
    }
}
