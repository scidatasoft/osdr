using Sds.Osdr.Generic.Domain.Events.UserNotifications;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Nodes
{
    public enum NodeType
    {
        User = 0,
        Folder,
        File,
        Record,
        Report,
        InvalidRecord,
        Model
    }

    public class NodePersisted<T> : IUserNotification
    {
        public NodePersisted(T payload, Guid id, Guid userId, string nodeType, Guid? parentId = null)
        {
            Id = NodeId = id;
            UserId = userId;
            NodeType = nodeType;
            ParentNodeId = parentId;
            EventPayload = payload;
            TimeStamp = DateTime.Now.ToUniversalTime();
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string NodeType { get; }

        public Guid NodeId { get; }

        public Guid? ParentNodeId { get; }

        public string AssemblyQualifiedEventTypeName => EventPayload?.GetType().AssemblyQualifiedName;

        public string EventTypeName => EventPayload?.GetType().Name;

        public object EventPayload { get; }

        public DateTime TimeStamp { get; }
    }
}
