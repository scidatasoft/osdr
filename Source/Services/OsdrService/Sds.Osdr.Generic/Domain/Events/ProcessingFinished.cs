using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events
{
    public class ProcessingFinished : IUserEvent
    {
        public ProcessingFinished(Guid id, Guid userId, Guid parentId, string nodeType, string originalEventName, object message)
        {
            Id = id;
            UserId = userId;
            ParentId = parentId;
            NodeType = nodeType;
            Message = message;
            OriginalEventName = originalEventName;
        }

        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public Guid UserId { get; set; }
        public string NodeType { get; }
        public object Message { get; }
        public string OriginalEventName { get; }
        public Guid ParentId { get; set; }

    }
}
