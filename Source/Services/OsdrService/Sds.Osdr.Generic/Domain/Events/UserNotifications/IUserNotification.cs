using System;

namespace Sds.Osdr.Generic.Domain.Events.UserNotifications
{
    public interface IUserNotification
    {
        object EventPayload { get; }
        string EventTypeName { get; }
        string AssemblyQualifiedEventTypeName { get; }
        Guid UserId { get; }
        string NodeType { get; }
        Guid NodeId { get; }
        Guid? ParentNodeId { get; }
    }
}
