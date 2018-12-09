using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.EventHandlers.Nodes
{
    public class RecordEventHandlers : IConsumer<NodePermissionChangedPersisted>
    {
        private readonly ISession _session;

        public RecordEventHandlers(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<NodePermissionChangedPersisted> context)
        {
            var message = context.Message;
            Type type = typeof(PermissionsChanged);
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Record", context.Message.Id, context.Message, type.Name, type.AssemblyQualifiedName, context.Message.ParentId));

            await _session.Commit();
        }
    }
}
