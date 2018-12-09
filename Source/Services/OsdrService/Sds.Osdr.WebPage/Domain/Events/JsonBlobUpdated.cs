using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.WebPage.Domain.Events
{
    public class JsonBlobUpdated : IUserEvent
    {
        public readonly string Bucket;
        public readonly Guid BlobId;
        public readonly string Md5;
        public readonly long Length;
        public readonly string Url;

        public JsonBlobUpdated(Guid id, string url,  Guid userId, string bucket, Guid blobId,
            long lenght, string md5)
        {
            Id = id;
            Url = url;
            UserId = userId;
            Bucket = bucket;
            BlobId = blobId;
            Length = lenght;
            Md5 = md5;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
