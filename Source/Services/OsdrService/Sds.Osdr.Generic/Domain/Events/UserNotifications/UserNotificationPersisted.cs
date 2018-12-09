using System;

namespace Sds.Osdr.Generic.Domain.Events.UserNotifications
{
    public interface UserNotificationPersisted
    {
        Guid Id { get; }
        IUserNotification Notification { get; }
    }
}

