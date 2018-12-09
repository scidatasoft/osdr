using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events
{
    public class ProcessingStarted : IUserEvent
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid? ParentId { get; set; }
        public string OriginalEventName { get; set; }
        public object Message { get; set; }
        public string NodeType { get; set; }
        public Guid UserId { get; set; }

        public ProcessingStarted(Guid id, Guid userId, Guid parentId, string nodeType, string originalEventName, object message)
        {
            Id = id;
            UserId = userId;
            ParentId = parentId;
            NodeType = nodeType;
            Message = message;
            OriginalEventName = originalEventName;
        }
    }
}
