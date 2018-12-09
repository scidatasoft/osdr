using MassTransit;
using Sds.CqrsLite.Events;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class ModelBlobChanged : CorrelatedBy<Guid>, IUserEvent
    {
        public Guid Id { get; set; }
        public string Bucket { get; set; }
        public Guid BlobId { get; set; }
        public IDictionary<string, object> Metadata { get; set; }

        public ModelBlobChanged(Guid id, Guid userId, Guid blobId, string bucket, IDictionary<string, object> metadata)
        {
            Id = id;
            UserId = userId;
            BlobId = blobId;
            Bucket = bucket;
            Metadata = metadata;
        }

        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public Guid UserId { get; set; }

        public Guid CorrelationId { get; set; }
    }
}
