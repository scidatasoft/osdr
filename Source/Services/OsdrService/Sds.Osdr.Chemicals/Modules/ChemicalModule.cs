using CQRSlite.Domain;
using MassTransit;
using MassTransit.RabbitMqTransport;
using MassTransit.Saga;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sds.CqrsLite.MassTransit.Filters;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Saga;
using Sds.MassTransit.Settings;
using Sds.Osdr.Chemicals.Sagas;
using Sds.Osdr.Chemicals.Sagas.Commands;
using Sds.Osdr.Domain.Modules;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Storage.Blob.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.Chemicals.Modules
{
    public class ChemicalModule : IModule
    {
        private readonly ISession _session;
        private readonly IBusControl _bus;

        public ChemicalModule(ISession session, IBusControl bus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public bool IsSupported(BlobLoaded blob)
        {
            return (new string[] { ".mol", ".sdf", ".cdx" }).Contains(Path.GetExtension(blob.BlobInfo.FileName).ToLower());
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

            await _bus.Publish<ProcessChemicalFile>(new
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
            services.AddScoped<IModule, ChemicalModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.CreateSubstanceCommandHandler>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.RecordsEventHandlers>();

            //  add state machines...
            services.AddSingleton<ChemicalFileProcessingStateMachine>();
            services.AddSingleton<InvalidSubstanceProcessingStateMachine>();
            services.AddSingleton<SubstanceProcessingStateMachine>();

            //  add state machines repositories...
            services.AddSingleton<ISagaRepository<ChemicalFileProcessingState>>(new InMemorySagaRepository<ChemicalFileProcessingState>());
            services.AddSingleton<ISagaRepository<InvalidSubstanceProcessingState>>(new InMemorySagaRepository<InvalidSubstanceProcessingState>());
            services.AddSingleton<ISagaRepository<SubstanceProcessingState>>(new InMemorySagaRepository<SubstanceProcessingState>());
        }

        public static void UseBackEndModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, ChemicalModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.CreateSubstanceCommandHandler>();
        }

        public static void UsePersistenceModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, ChemicalModule>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.RecordsEventHandlers>();
        }

        public static void UseSagaHostModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, ChemicalModule>();

            //  add state machines...
            services.AddSingleton<ChemicalFileProcessingStateMachine>();
            services.AddSingleton<InvalidSubstanceProcessingStateMachine>();
            services.AddSingleton<SubstanceProcessingStateMachine>();
        }
    }

    public static class ConfigurationExtensions
    {
        public static void RegisterInMemoryModule(this IBusFactoryConfigurator bus, IServiceProvider provider)
        {
            bus.RegisterScopedConsumer<BackEnd.CommandHandlers.CreateSubstanceCommandHandler>(provider, null, c => c.UseCqrsLite());

            bus.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(provider);
            bus.RegisterConsumer<Persistence.EventHandlers.RecordsEventHandlers>(provider);

            bus.RegisterStateMachine<ChemicalFileProcessingStateMachine, ChemicalFileProcessingState>(provider);
            bus.RegisterStateMachine<InvalidSubstanceProcessingStateMachine, InvalidSubstanceProcessingState>(provider);
            bus.RegisterStateMachine<SubstanceProcessingStateMachine, SubstanceProcessingState>(provider);
        }

        public static void RegisterBackEndModule(this IRabbitMqBusFactoryConfigurator bus, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register backend consumers...
            bus.RegisterScopedConsumer<BackEnd.CommandHandlers.CreateSubstanceCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
        }

        public static void RegisterPersistenceModule(this IRabbitMqBusFactoryConfigurator bus, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register persistence consumers...
            bus.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(host, provider, endpointConfigurator);
            bus.RegisterConsumer<Persistence.EventHandlers.RecordsEventHandlers>(host, provider, endpointConfigurator);
        }

        public static void RegisterSagaHostModule(this IRabbitMqBusFactoryConfigurator bus, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            var repositoryFactory = provider.GetRequiredService<ISagaRepositoryFactory>();

            //  register state machines...
            bus.RegisterStateMachine<ChemicalFileProcessingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
            bus.RegisterStateMachine<InvalidSubstanceProcessingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
            bus.RegisterStateMachine<SubstanceProcessingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
        }
    }
}
