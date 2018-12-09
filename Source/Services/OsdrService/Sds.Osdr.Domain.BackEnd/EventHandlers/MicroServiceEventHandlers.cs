using CQRSlite.Domain;
using MassTransit;
using Sds.ChemicalExport.Domain.Events;
using Sds.Osdr.Generic.Domain;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Domain.BackEnd.EventHandlers
{
    public class MicroServiceEventHandlers : IConsumer<ExportFinished>//, IConsumer<IUserNotification>
    {
        private readonly ISession _session;

        public MicroServiceEventHandlers(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<ExportFinished> context)
        {
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "File", context.Message.UserId, context.Message, typeof(ExportFinished).Name, typeof(ExportFinished).AssemblyQualifiedName, null));

            await _session.Commit();
        }

        //public async Task Consume(ConsumeContext<IUserNotification> context)
        //{
        //    var message = context.Message;
        //    await _session.Add(new UserNotification(NewId.NextGuid(), context.Message));

        //    await _session.Commit();
        //}
    }
}
