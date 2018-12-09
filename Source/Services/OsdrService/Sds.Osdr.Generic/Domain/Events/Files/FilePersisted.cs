using CQRSlite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class FilePersisted : IEvent
    {
        public readonly string FileName;
        public readonly string Bucket;

        public FilePersisted(Guid id, Guid userId, string fileName, string bucket)
        {
            Id = id;
            UserId = userId;
            FileName = fileName;
            Bucket = bucket;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
