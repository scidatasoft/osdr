using CQRSlite.Domain;
using MassTransit;
using MassTransit.RabbitMqTransport;
using MassTransit.Saga;
using Microsoft.Extensions.DependencyInjection;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Extensions;
using Sds.Osdr.Domain.Modules;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Osdr.Spectra.Sagas;
using Sds.Osdr.Spectra.Sagas.Commands;
using Sds.Storage.Blob.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sds.MassTransit.Saga;
using Sds.CqrsLite.MassTransit.Filters;

namespace Sds.Osdr.Spectra.Modules
{
    public class SpectrumModule : IModule
    {
        private readonly ISession _session;
        private readonly IBusControl _bus;

        public SpectrumModule(ISession session, IBusControl bus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public bool IsSupported(BlobLoaded blob)
        {
            return (new string[] { ".dx", ".jdx" }).Contains(Path.GetExtension(blob.BlobInfo.FileName).ToLower());
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

            await _bus.Publish<ProcessSpectralFile>(new
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
            services.AddScoped<IModule, SpectrumModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.CreateSpectrumCommandHandler>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.RecordsEventHandlers>();

            //  add state machines...
            services.AddSingleton<SpectrumProcessingStateMachine>();
            services.AddSingleton<SpectrumFileProcessingStateMachine>();

            //  add state machines repositories...
            services.AddSingleton<ISagaRepository<SpectrumProcessingState>>(new InMemorySagaRepository<SpectrumProcessingState>());
            services.AddSingleton<ISagaRepository<SpectrumFileProcessingState>>(new InMemorySagaRepository<SpectrumFileProcessingState>());
        }

        public static void UseBackEndModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, SpectrumModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.CreateSpectrumCommandHandler>();
        }

        public static void UsePersistenceModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, SpectrumModule>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.RecordsEventHandlers>();
        }

        public static void UseSagaHostModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, SpectrumModule>();

            //  add state machines...
            services.AddSingleton<SpectrumFileProcessingStateMachine>();
            services.AddSingleton<SpectrumProcessingStateMachine>();
        }
    }

    public static class ConfigurationExtensions
    {
        public static void RegisterInMemoryModule(this IBusFactoryConfigurator configurator, IServiceProvider provider)
        {
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.CreateSpectrumCommandHandler>(provider, null, c => c.UseCqrsLite());

            configurator.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.RecordsEventHandlers>(provider);

            configurator.RegisterStateMachine<SpectrumFileProcessingStateMachine, SpectrumFileProcessingState>(provider);
            configurator.RegisterStateMachine<SpectrumProcessingStateMachine, SpectrumProcessingState>(provider);
        }

        public static void RegisterBackEndModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register backend consumers...
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.CreateSpectrumCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
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
            configurator.RegisterStateMachine<SpectrumFileProcessingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
            configurator.RegisterStateMachine<SpectrumProcessingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
        }
    }
}
