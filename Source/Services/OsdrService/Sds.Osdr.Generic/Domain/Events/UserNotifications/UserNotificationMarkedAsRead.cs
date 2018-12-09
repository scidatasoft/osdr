using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.UserNotifications
{
    internal class UserNotificationMarkedAsRead : IUserEvent
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; }

        public UserNotificationMarkedAsRead(Guid id)
        {
            Id = id;
        }
    }
}