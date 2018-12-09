using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class InvalidRecordCreated : IUserEvent
    {
        public RecordType RecordType { get; set; }
        public readonly Guid FileId;
        public readonly long Index;
        public readonly string Error;

        public InvalidRecordCreated(Guid id, Guid userId, RecordType recordType, Guid fileId, long index, string error)
        {
            Id = id;
            UserId = userId;
            RecordType = recordType;
            FileId = fileId;
            Index = index;
            Error = error;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
