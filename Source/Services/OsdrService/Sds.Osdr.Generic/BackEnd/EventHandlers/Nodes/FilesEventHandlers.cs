using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Events.Nodes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.BackEnd.EventHandlers.Nodes
{
    public class FilesEventHandlers : IConsumer<PermissionChangedPersisted>,
                                      IConsumer<Domain.Events.Files.NodeStatusPersisted>,
                                      IConsumer<FilePersisted>,
                                      IConsumer<MovedFilePersisted>, 
                                      IConsumer<RenamedFilePersisted>,
                                      IConsumer<DeletedFilePersisted>
    {
        private readonly ISession _session;

        public FilesEventHandlers(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<PermissionChangedPersisted> context)
        {
            var message = context.Message;
            Type type = typeof(Generic.Domain.Events.Files.PermissionsChanged);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "File", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.ParentId));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<FilePersisted> context)
        {
            var message = context.Message;
            Type type = typeof(Generic.Domain.Events.Files.FileCreated);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "File", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.ParentId));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<MovedFilePersisted> context)
        {
            var message = context.Message;
            Type type = typeof(Generic.Domain.Events.Files.FileMoved);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "File", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.OldParentId));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<RenamedFilePersisted> context)
        {
            var message = context.Message;
            Type type = typeof(Generic.Domain.Events.Files.FileNameChanged);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "File", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.ParentId));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<DeletedFilePersisted> context)
        {
            var message = context.Message;
            Type type = typeof(Generic.Domain.Events.Files.FileDeleted);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "File", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.ParentId));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<Generic.Domain.Events.Files.NodeStatusPersisted> context)
        {
            if (new[] { FileStatus.Processed, FileStatus.Failed }.Contains(context.Message.Status))
            {
                Type type = typeof(Generic.Domain.Events.Files.StatusChanged);
                await _session.Add(new UserNotification(
                    NewId.NextGuid(), context.Message.UserId, "File", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.ParentId));

                await _session.Commit();
            }
        }
    }
}
