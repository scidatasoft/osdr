using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class StatusPersisted : IUserEvent
    {
        public readonly FileStatus Status;

        public StatusPersisted(Guid id, Guid userId, FileStatus status)
        {
            Id = id;
            UserId = userId;
            Status = status;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
