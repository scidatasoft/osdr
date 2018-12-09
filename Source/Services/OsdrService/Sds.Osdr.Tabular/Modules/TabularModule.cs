using CQRSlite.Domain;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.RabbitMq;
using Sds.Osdr.Domain.Modules;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Sagas.Commands;
using Sds.Osdr.Tabular.Domain;
using Sds.Storage.Blob.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.Tabular.Modules
{
    public class TabularModule : IModule
    {
        private readonly ISession _session;
        private readonly IBusControl _bus;

        public TabularModule(ISession session, IBusControl bus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public bool IsSupported(BlobLoaded blob)
        {
            return (new string[] { ".csv", ".tsv" }).Contains(Path.GetExtension(blob.BlobInfo.FileName).ToLower());
        }

        public async Task Process(BlobLoaded blob)
        {
            var fileId = NewId.NextGuid();
            var blobInfo = blob.BlobInfo;
            Guid userId = blobInfo.UserId.HasValue ? blobInfo.UserId.Value : new Guid(blobInfo.Metadata[nameof(userId)].ToString());
            Guid? parentId = blobInfo.Metadata != null ? blobInfo.Metadata.ContainsKey(nameof(parentId)) ? (Guid?)new Guid(blobInfo.Metadata[nameof(parentId)].ToString()) : null : null;

            var file = new TabularFile(fileId, userId, parentId, blobInfo.FileName, FileStatus.Loaded, blobInfo.Bucket, blobInfo.Id, blobInfo.Length, blobInfo.MD5);
            await _session.Add(file);
            await _session.Commit();

            await _bus.Publish<ProcessGenericFile>(new
            {
                Id = fileId,
                ParentId = parentId,
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
            services.AddTransient<IModule, TabularModule>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.FilesEventHandlers>();
        }

        public static void UseBackEndModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, TabularModule>();
        }

        public static void UsePersistenceModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, TabularModule>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.FilesEventHandlers>();
        }
    }

    public static class ConfigurationExtensions
    {
        public static void RegisterInMemoryModule(this IBusFactoryConfigurator configurator, IServiceProvider provider)
        {
            configurator.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.FilesEventHandlers>(provider);
        }

        public static void RegisterPersistenceModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register persistence consumers...
            configurator.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.FilesEventHandlers>(host, provider, endpointConfigurator);
        }
    }
}
