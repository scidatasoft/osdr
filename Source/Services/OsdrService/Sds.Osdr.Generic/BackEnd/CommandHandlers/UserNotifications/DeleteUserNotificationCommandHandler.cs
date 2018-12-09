using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.UserNotifications;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.BackEnd.CommandHandlers.UserNotifications
{
    public class DeleteUserNotificationCommandHandler : IConsumer<DeleteUserNotification>
    {
        private readonly ISession _session;

        public DeleteUserNotificationCommandHandler(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }
        public async Task Consume(ConsumeContext<DeleteUserNotification> context)
        {
            var notification = await _session.Get<UserNotification>(context.Message.Id);
            notification.Delete();

            await _session.Commit();
        }
    }
}
