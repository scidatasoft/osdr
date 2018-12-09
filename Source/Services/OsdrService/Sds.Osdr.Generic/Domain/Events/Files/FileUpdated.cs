using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class FileUpdated : IUserEvent
    {
        public readonly string Bucket;
        public readonly Guid BlobId;
        public readonly Guid ParentId;
        public readonly string FileName;
        public readonly long Length;
        public readonly string Md5;

        public FileUpdated(Guid id, string bucket, Guid blobId, Guid userId, Guid parentId, string fileName, long length, string md5)
        {
            Id = id;
            Bucket = bucket;
            BlobId = blobId;
            UserId = userId;
            ParentId = parentId;
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
