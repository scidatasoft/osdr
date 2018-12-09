using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.WebPage.Domain.Events
{
    public class WebPageUpdated : IUserEvent
    {
        public readonly string Bucket;
        public readonly Guid JsonBlobId;

        public WebPageUpdated(Guid id, Guid userId, string bucket, Guid jsonBlobId)
        {
            Id = id;
            UserId = userId;
            Bucket = bucket;
            JsonBlobId = jsonBlobId;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
