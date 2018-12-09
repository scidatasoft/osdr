using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Files
{
    public class RecordsFileCreated : IUserEvent
    {
		public RecordsFileCreated(Guid id, Guid userId)
		{
			Id = id;
            UserId = userId;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
	}
}
