using Sds.CqrsLite.Commands;
using System;

namespace Sds.PdfProcessor.Domain.Commands
{
    public class ExtractMeta : ICorrelatedCommand
    {
        public readonly string Bucket;
        public readonly Guid BlobId;

        public ExtractMeta(Guid id, Guid correlationId, Guid userId, string bucket, Guid blobId)
        {
            Id = id;
            Bucket = bucket;
            BlobId = blobId;
            CorrelationId = correlationId;
            UserId = userId;
        }

        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public int ExpectedVersion { get; set; }
    }
}
