using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Sds.Osdr.Generic.Domain.Events.UserNotifications;
using Sds.Osdr.WebApi.Hubs;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.EventHandlers
{
    public class UserNotificationPersistedEventHandlers : IConsumer<UserNotificationPersisted>
    {
        IHubContext<OrganizeHub> _hubContext;
        public UserNotificationPersistedEventHandlers(IHubContext<OrganizeHub> hubContext)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }
        public async Task Consume(ConsumeContext<UserNotificationPersisted> context)
        {
            var notification = context.Message.Notification;

            await _hubContext.Clients.User(notification.UserId.ToString())
                .SendAsync("updateNotficationBar",
                    new
                    {
                        id = context.Message.Id,
                        nodeId = notification.NodeId,
                        nodeType = notification.NodeType,
                        eventName = notification.EventTypeName,
                        eventPayload = notification.EventPayload
                    });
        }
    }
}
