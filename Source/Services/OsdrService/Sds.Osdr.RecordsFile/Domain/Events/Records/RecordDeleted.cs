using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class RecordDeleted : IUserEvent
    {
        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public Guid UserId { get; }
        public RecordType RecordType { get; }
        public bool Force { get; }

        public RecordDeleted(Guid id, RecordType recordType, Guid userId, bool force)
        {
            Id = id;
            RecordType = recordType;
			UserId = userId;
            Force = force;
        }
    }
}
