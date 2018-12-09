using System;
using CQRSlite.Events;

namespace Sds.Osdr.Generic.Domain.Events.UserNotifications
{
    public class UserNotificationDeleted : IEvent
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }

        public UserNotificationDeleted(Guid id)
        {
            Id = id;
        }
    }
}