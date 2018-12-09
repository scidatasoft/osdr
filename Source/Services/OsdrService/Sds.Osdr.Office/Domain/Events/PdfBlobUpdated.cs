using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Office.Domain.Events
{
    public class PdfBlobUpdated : IUserEvent
    {
        public readonly string Bucket;
        public readonly Guid BlobId;
        public readonly long Lenght;
        public readonly string Md5;

        public PdfBlobUpdated(Guid id, Guid userId, string bucket, Guid blobId, long lenght = 0, string md5 = "")
        {
            Id = id;
            UserId = userId;
            Bucket = bucket;
            BlobId = blobId;
            Lenght = lenght;
            Md5 = md5;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
