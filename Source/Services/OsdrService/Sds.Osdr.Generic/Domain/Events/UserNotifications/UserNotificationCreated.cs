using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.UserNotifications
{
    public class UserNotificationCreated : IUserEvent
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; }
        public object EventPayload { get; }
        public string EventTypeName { get; }
        public string AssemblyQualifiedEventTypeName { get; }
        public string NodeType { get; }
        public Guid NodeId { get; }
        public Guid? ParentNodeId { get; }

        public UserNotificationCreated(Guid id, Guid userId, string nodeType, Guid nodeId, object eventPayload, string eventTypeName, string assemblyQualifiedEventTypeName, Guid? parentNodeId)
        {
            Id = id;
            UserId = userId;
            EventPayload = eventPayload;
            EventTypeName = eventTypeName;
            AssemblyQualifiedEventTypeName = assemblyQualifiedEventTypeName;
            NodeType = nodeType;
            NodeId = nodeId;
            ParentNodeId = parentNodeId;
        }
    }
}
