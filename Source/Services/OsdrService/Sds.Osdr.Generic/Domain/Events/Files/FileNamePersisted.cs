using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class FileNamePersisted : IUserEvent
    {
        public readonly string Name;

        public FileNamePersisted(Guid id, Guid userId, string name)
        {
            Id = id;
            UserId = userId;
            Name = name;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
