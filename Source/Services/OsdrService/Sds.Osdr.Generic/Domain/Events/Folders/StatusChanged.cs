using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Folders
{
    public class StatusChanged : IUserEvent
    {
        public FolderStatus Status { get; set; }

        public StatusChanged(Guid id, Guid userId, FolderStatus status)
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
