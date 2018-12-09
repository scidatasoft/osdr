using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class ProcessingProgressChanged : IUserEvent
    {
        public readonly long Total;
        public readonly long Processed;
        public readonly long Failed;

        public ProcessingProgressChanged(Guid id, Guid userId, long total, long processed, long failed)
        {
            Id = id;
            UserId = userId;
            Total = total;
            Processed = processed;
            Failed = failed;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
