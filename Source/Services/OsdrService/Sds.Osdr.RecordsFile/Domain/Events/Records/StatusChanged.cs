using CQRSlite.Events;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class StatusChanged : IEvent
    {
        public Guid FileId { get; set; }
        public RecordStatus Status { get; set; }

        public StatusChanged(Guid id, Guid userId, Guid fileId, RecordStatus status)
        {
            Id = id;
            UserId = userId;
            FileId = fileId;
            Status = status;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
