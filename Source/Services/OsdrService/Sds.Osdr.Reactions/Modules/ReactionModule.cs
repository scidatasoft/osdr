using CQRSlite.Domain;
using MassTransit;
using MassTransit.Saga;
using Microsoft.Extensions.DependencyInjection;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Extensions;
using Sds.Osdr.Domain.Modules;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Osdr.Reactions.Sagas;
using Sds.Osdr.Reactions.Sagas.Commands;
using Sds.Storage.Blob.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MassTransit.RabbitMqTransport;
using Sds.MassTransit.Saga;
using Sds.CqrsLite.MassTransit.Filters;

namespace Sds.Osdr.Reactions.Modules
{
    public class ReactionModule : IModule
    {
        private readonly ISession _session;
        private readonly IBusControl _bus;

        public ReactionModule(ISession session, IBusControl bus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public bool IsSupported(BlobLoaded blob)
        {
            return (new string[] { ".rdf", ".rxn" }).Contains(Path.GetExtension(blob.BlobInfo.FileName).ToLower());
        }

        public async Task Process(BlobLoaded blob)
        {
            var fileId = NewId.NextGuid();
            var blobInfo = blob.BlobInfo;
            Guid userId = blobInfo.UserId.HasValue ? blobInfo.UserId.Value : new Guid(blobInfo.Metadata[nameof(userId)].ToString());
            Guid? parentId = blobInfo.Metadata != null ? blobInfo.Metadata.ContainsKey(nameof(parentId)) ? (Guid?)new Guid(blobInfo.Metadata[nameof(parentId)].ToString()) : null : null;

            var file = new RecordsFile.Domain.RecordsFile(fileId, userId, parentId, blobInfo.FileName, FileStatus.Loaded, blobInfo.Bucket, blobInfo.Id, blobInfo.Length, blobInfo.MD5);
            await _session.Add(file);
            await _session.Commit();

            await _bus.Publish<ProcessReactionFile>(new
            {
                Id = fileId,
                Bucket = blobInfo.Bucket,
                BlobId = blobInfo.Id,
                UserId = userId
            });
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static void UseInMemoryModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, ReactionModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.CreateReactionCommandHandler>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.RecordsEventHandlers>();

            //  add state machines...
            services.AddSingleton<ReactionProcessingStateMachine>();
            services.AddSingleton<ReactionFileProcessingStateMachine>();

            //  add state machines repositories...
            services.AddSingleton<ISagaRepository<ReactionProcessingState>>(new InMemorySagaRepository<ReactionProcessingState>());
            services.AddSingleton<ISagaRepository<ReactionFileProcessingState>>(new InMemorySagaRepository<ReactionFileProcessingState>());
        }

        public static void UseBackEndModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, ReactionModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.CreateReactionCommandHandler>();
        }

        public static void UsePersistenceModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, ReactionModule>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.RecordsEventHandlers>();
        }

        public static void UseSagaHostModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, ReactionModule>();

            //  add state machines...
            services.AddSingleton<ReactionFileProcessingStateMachine>();
            services.AddSingleton<ReactionProcessingStateMachine>();
        }
    }

    public static class ConfigurationExtensions
    {
        public static void RegisterInMemoryModule(this IBusFactoryConfigurator configurator, IServiceProvider provider)
        {
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.CreateReactionCommandHandler>(provider, null, c => c.UseCqrsLite());

            configurator.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.RecordsEventHandlers>(provider);

            configurator.RegisterStateMachine<ReactionFileProcessingStateMachine, ReactionFileProcessingState>(provider);
            configurator.RegisterStateMachine<ReactionProcessingStateMachine, ReactionProcessingState>(provider);
        }

        public static void RegisterBackEndModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register backend consumers...
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.CreateReactionCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
        }

        public static void RegisterPersistenceModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register persistence consumers...
            configurator.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.RecordsEventHandlers>(host, provider, endpointConfigurator);
        }

        public static void RegisterSagaHostModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            var repositoryFactory = provider.GetRequiredService<ISagaRepositoryFactory>();

            //  register state machines...
            configurator.RegisterStateMachine<ReactionFileProcessingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
            configurator.RegisterStateMachine<ReactionProcessingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
        }
    }
}
