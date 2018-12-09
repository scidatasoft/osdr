using Sds.CqrsLite.Events;
using System;
using System.Collections.Generic;

namespace Sds.PdfProcessor.Domain.Events
{
    public class MetaExtracted : ICorrelatedEvent
    {
        public string Bucket { get; set; }
        public Guid BlobId { get; set; }
        public Dictionary<string, object> Meta { get; set; }

        public MetaExtracted(Guid id, Guid correlationId, Guid userId, Dictionary<string, object> meta, string bucket, Guid blobId)
        {
            Id = id;
            BlobId = blobId;
            Meta = meta;
            Bucket = bucket;
            CorrelationId = correlationId;
            UserId = userId;
            TimeStamp = DateTime.Now;
        }


        public Guid Id { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public int Version { get; set; }

        public Guid CorrelationId { get; set; }

        public Guid UserId { get; set; }
    }
}
