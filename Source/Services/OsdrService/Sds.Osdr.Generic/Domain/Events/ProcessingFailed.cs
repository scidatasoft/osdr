using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events
{
    public class ProcessingFailed : IUserEvent
    {
        public ProcessingFailed(Guid id, Guid userId, Guid parentId, string nodeType, object message)
        {
            Id = id;
            UserId = userId;
            ParentId = parentId;
            NodeType = nodeType;
            Message = message;
        }

        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public Guid UserId { get; set; }
        public string NodeType { get; }
        public object Message { get; }
        public Guid ParentId { get; set; }

    }
}