using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Events.Nodes;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.BackEnd.EventHandlers.Nodes
{
    public class FoldersEventHandlers : IConsumer<FolderPersisted>,
                                        IConsumer<MovedFolderPersisted>,
                                        IConsumer<DeletedFolderPersisted>,
                                        IConsumer<RenamedFolderPersisted>
    {
        private readonly ISession _session;

        public FoldersEventHandlers(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<FolderPersisted> context)
        {
            var message = context.Message;
            Type type = typeof(Generic.Domain.Events.Folders.FolderCreated);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Folder", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.ParentId));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<MovedFolderPersisted> context)
        {
            var message = context.Message;
            Type type = typeof(Generic.Domain.Events.Folders.FolderMoved);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Folder", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.OldParentId));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<RenamedFolderPersisted> context)
        {
            var message = context.Message;
            Type type = typeof(Generic.Domain.Events.Folders.FolderNameChanged);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Folder", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.ParentId));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<DeletedFolderPersisted> context)
        {
            var message = context.Message;
            Type type = typeof(Generic.Domain.Events.Folders.FolderDeleted);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Folder", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.ParentId));

            await _session.Commit();
        }
    }
}
