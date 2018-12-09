using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class BlobUpdated : IUserEvent
    {
        public readonly string Bucket;
        public readonly Guid BlobId;
        public readonly string FileName;
        public readonly long Length;
        public readonly string Md5;

        public BlobUpdated(Guid id, Guid userId, string fileName, string bucket, Guid blobId, long length, string md5)
        {
            Id = id;
            UserId = userId;
            BlobId = blobId;
            TimeStamp = DateTime.Now;
            FileName = fileName;
            Length = length;
            Md5 = md5;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
