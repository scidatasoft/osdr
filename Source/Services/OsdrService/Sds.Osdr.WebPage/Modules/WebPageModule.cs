using MassTransit;
using MassTransit.RabbitMqTransport;
using MassTransit.Saga;
using Microsoft.Extensions.DependencyInjection;
using Sds.CqrsLite.MassTransit.Filters;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Saga;
using Sds.Osdr.Domain.Modules;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Osdr.WebPage.Sagas;
using Sds.Storage.Blob.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.WebPage.Modules
{
    public class WebPageModule : IModule
    {
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
            services.AddScoped<IModule, WebPageModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.CreateWebPageCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.UpdateTotalRecordsCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.UpdateWebPageCommandHandler>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.FilesEventHandlers>();

            //  add state machines...
            services.AddSingleton<WebPageProcessingStateMachine>();

            //  add state machines repositories...
            services.AddSingleton<ISagaRepository<WebPageProcessingState>>(new InMemorySagaRepository<WebPageProcessingState>());
        }

        public static void UseBackEndModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, WebPageModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.CreateWebPageCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.UpdateTotalRecordsCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.UpdateWebPageCommandHandler>();
        }

        public static void UsePersistenceModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, WebPageModule>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.FilesEventHandlers>();
        }

        public static void UseSagaHostModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, WebPageModule>();

            //  add state machines...
            services.AddSingleton<WebPageProcessingStateMachine>();
        }
    }

    public static class ConfigurationExtensions
    {
        public static void RegisterInMemoryModule(this IBusFactoryConfigurator configurator, IServiceProvider provider)
        {
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.CreateWebPageCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.UpdateTotalRecordsCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.UpdateWebPageCommandHandler>(provider, null, c => c.UseCqrsLite());

            configurator.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.FilesEventHandlers>(provider);

            configurator.RegisterStateMachine<WebPageProcessingStateMachine, WebPageProcessingState>(provider);
        }

        public static void RegisterBackEndModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register backend consumers...
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.CreateWebPageCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.UpdateTotalRecordsCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.UpdateWebPageCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
        }

        public static void RegisterPersistenceModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register persistence consumers...
            configurator.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.FilesEventHandlers>(host, provider, endpointConfigurator);
        }

        public static void RegisterSagaHostModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            var repositoryFactory = provider.GetRequiredService<ISagaRepositoryFactory>();

            //  register state machines...
            configurator.RegisterStateMachine<WebPageProcessingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
        }
    }
}
