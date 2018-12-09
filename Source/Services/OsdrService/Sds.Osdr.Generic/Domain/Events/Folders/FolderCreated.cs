using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Folders
{
    public class FolderCreated : IUserEvent, ISessionEvent, ICorrelatedEvent
	{
        public Guid? ParentId { get; set; }
        public string Name { get; set; }
        public FolderStatus Status { get; set; }
        public Guid SessionId { get; set; }
        public Guid UserId { get; set; }

        public FolderCreated(Guid id, Guid correlationId, Guid userId, Guid? parentId, string name, Guid sessionId, FolderStatus status)
        {
            Id = id;
			CorrelationId = correlationId;
			UserId = userId;
            ParentId = parentId;
            Name = name;
            SessionId = sessionId;
			Status = status;
		}

        public Guid Id { get; set; }
		public Guid CorrelationId { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
	}
}
