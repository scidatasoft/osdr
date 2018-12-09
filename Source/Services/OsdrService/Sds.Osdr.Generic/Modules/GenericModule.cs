using CQRSlite.Domain;
using MassTransit;
using MassTransit.RabbitMqTransport;
using MassTransit.Saga;
using Microsoft.Extensions.DependencyInjection;
using Sds.CqrsLite.MassTransit.Filters;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Saga;
using Sds.Osdr.Domain.Modules;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Sagas;
using Sds.Osdr.Generic.Sagas.Commands;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Storage.Blob.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Modules
{
    public class GenericModule : IModule
    {
        private readonly ISession _session;
        private readonly IBusControl _bus;

        public GenericModule(ISession session, IBusControl bus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public bool IsSupported(BlobLoaded blob)
        {
            return true;
        }

        public async Task Process(BlobLoaded blob)
        {
            var fileId = NewId.NextGuid();
            var blobInfo = blob.BlobInfo;
            Guid userId = blobInfo.UserId.HasValue ? blobInfo.UserId.Value : new Guid(blobInfo.Metadata[nameof(userId)].ToString());
            //TODO: is it possible that parentId is nul???
            Guid? parentId = blobInfo.Metadata != null ? blobInfo.Metadata.ContainsKey(nameof(parentId)) ? (Guid?)new Guid(blobInfo.Metadata[nameof(parentId)].ToString()) : null : null;

            var file = new File(fileId, userId, parentId, blobInfo.FileName, FileStatus.Loaded, blobInfo.Bucket, blobInfo.Id, blobInfo.Length, blobInfo.MD5, FileType.Generic);
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
            services.AddScoped<IModule, GenericModule>();

            //  add frontend consumers...
            services.AddScoped<FrontEnd.CommandHandlers.FilesCommandHandler>();
            services.AddScoped<FrontEnd.CommandHandlers.FoldersCommandHandler>();
            services.AddScoped<FrontEnd.CommandHandlers.UsersCommandHandler>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.Files.AddImageCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Files.ChangeStatusCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Files.GrantAccessCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Folders.GrantAccessCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.UserNotifications.DeleteUserNotificationCommandHandler>();

            services.AddScoped<BackEnd.EventHandlers.Nodes.FilesEventHandlers>();
            services.AddScoped<BackEnd.EventHandlers.Nodes.FoldersEventHandlers>();
            services.AddScoped<BackEnd.EventHandlers.BlobEventHandlers>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.Files.FileEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Folders.FolderEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Nodes.FileEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Nodes.FolderContentEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Nodes.UserEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Users.UserEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.UserNotifications.UserNotificationEventHandler>();

            //  add state machines...
            services.AddSingleton<GenericFileProcessingStateMachine>();

            //  add state machines repositories...
            services.AddSingleton<ISagaRepository<GenericFileProcessingState>>(new InMemorySagaRepository<GenericFileProcessingState>());
        }

        public static void UseBackEndModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, GenericModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.CommandHandlers.Files.AddImageCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Files.ChangeStatusCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Files.GrantAccessCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.Folders.GrantAccessCommandHandler>();
            services.AddScoped<BackEnd.CommandHandlers.UserNotifications.DeleteUserNotificationCommandHandler>();

            services.AddScoped<BackEnd.EventHandlers.Nodes.FilesEventHandlers>();
            services.AddScoped<BackEnd.EventHandlers.Nodes.FoldersEventHandlers>();
            services.AddScoped<BackEnd.EventHandlers.BlobEventHandlers>();
        }

        public static void UseFrontEndModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, GenericModule>();

            //  add frontend consumers...
            services.AddScoped<FrontEnd.CommandHandlers.FilesCommandHandler>();
            services.AddScoped<FrontEnd.CommandHandlers.FoldersCommandHandler>();
            services.AddScoped<FrontEnd.CommandHandlers.UsersCommandHandler>();
        }

        public static void UsePersistenceModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, GenericModule>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.Files.FileEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Folders.FolderEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Nodes.FileEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Nodes.FolderContentEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Nodes.UserEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.Users.UserEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.UserNotifications.UserNotificationEventHandler>();
        }

        public static void UseSagaHostModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, GenericModule>();

            //  add state machines...
            services.AddSingleton<GenericFileProcessingStateMachine>();

            //  add state machines repositories...
            //services.AddSingleton<ISagaRepository<GenericFileProcessingState>>(new InMemorySagaRepository<GenericFileProcessingState>());
        }
    }

    public static class ConfigurationExtensions
    {
        public static void RegisterInMemoryModule(this IBusFactoryConfigurator configurator, IServiceProvider provider)
        {
            //  register frontend consumers
            configurator.RegisterScopedConsumer<FrontEnd.CommandHandlers.FilesCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<FrontEnd.CommandHandlers.FoldersCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<FrontEnd.CommandHandlers.UsersCommandHandler>(provider, null, c => c.UseCqrsLite());

            //  register backend consumers...
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.AddImageCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.ChangeStatusCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.GrantAccessCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Folders.GrantAccessCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.UserNotifications.DeleteUserNotificationCommandHandler>(provider, null, c => c.UseCqrsLite());

            configurator.RegisterScopedConsumer<BackEnd.EventHandlers.Nodes.FilesEventHandlers>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.EventHandlers.Nodes.FoldersEventHandlers>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.EventHandlers.BlobEventHandlers>(provider, null, c => c.UseCqrsLite());

            //  register persistence consumers...
            configurator.RegisterConsumer<Persistence.EventHandlers.Files.FileEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.Folders.FolderEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.Nodes.FileEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.Nodes.FolderContentEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.Nodes.UserEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.Users.UserEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.UserNotifications.UserNotificationEventHandler>(provider);

            //  register state machines...
            configurator.RegisterStateMachine<GenericFileProcessingStateMachine, GenericFileProcessingState>(provider);
        }

        public static void RegisterBackEndModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register backend consumers...
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.AddImageCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.ChangeStatusCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Files.GrantAccessCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.Folders.GrantAccessCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CommandHandlers.UserNotifications.DeleteUserNotificationCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());

            configurator.RegisterScopedConsumer<BackEnd.EventHandlers.Nodes.FilesEventHandlers>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.EventHandlers.Nodes.FoldersEventHandlers>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.EventHandlers.BlobEventHandlers>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
        }

        public static void RegisterFrontEndModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register frontend consumers
            configurator.RegisterScopedConsumer<FrontEnd.CommandHandlers.FilesCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<FrontEnd.CommandHandlers.FoldersCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<FrontEnd.CommandHandlers.UsersCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
        }

        public static void RegisterPersistenceModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register persistence consumers...
            configurator.RegisterConsumer<Persistence.EventHandlers.Files.FileEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.Folders.FolderEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.Nodes.FileEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.Nodes.FolderContentEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.Nodes.UserEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.Users.UserEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.UserNotifications.UserNotificationEventHandler>(host, provider, endpointConfigurator);
        }

        public static void RegisterSagaHostModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            var repositoryFactory = provider.GetRequiredService<ISagaRepositoryFactory>();

            //  register state machines...
            configurator.RegisterStateMachine<GenericFileProcessingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
        }
    }
}
