using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class FileDeleted : IUserEvent
    {
        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public FileType FileType { get; set; }
        public Guid UserId { get; set; }
        public bool Force { get; set; }

        public FileDeleted(Guid id, FileType fileType, Guid userId, bool force)
        {
            Id = id;
            FileType = fileType;
            UserId = userId;
            Force = force;
        }
    }
}
