using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Folders
{
    public class FolderDeleted : IUserEvent, ICorrelatedEvent
    {
        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public Guid UserId { get; }
		public Guid CorrelationId { get; set; }
        public bool Force { get; set; }

		public FolderDeleted(Guid id, Guid correlationId, Guid userId, bool force)
        {
            Id = id;
			CorrelationId = correlationId;
			UserId = userId;
            Force = force;
        }
    }
}
