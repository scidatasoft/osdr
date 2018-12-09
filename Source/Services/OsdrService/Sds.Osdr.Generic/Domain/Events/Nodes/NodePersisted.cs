using System;

namespace Sds.Osdr.Generic.Domain.Events.Nodes
{
    public class NodePersisted
    {
        public NodePersisted(Guid id, Guid userId, string nodeType, string orginalEventName, Guid? parentId = null, object originalEventData = null)
        {
            Id = id;
            UserId = userId;
            NodeType = nodeType;
            ParentId = parentId;
            OrginalEventName = orginalEventName;
            OriginalEventData = originalEventData;
        }

        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public Guid UserId { get; set; }
        public string NodeType { get; }
        public string OrginalEventName { get; }
        public Guid? ParentId { get; }
        public object OriginalEventData { get; }
    }
}
