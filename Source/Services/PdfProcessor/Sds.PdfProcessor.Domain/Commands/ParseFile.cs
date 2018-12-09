using Sds.CqrsLite.Commands;
using System;
using System.Collections.Generic;

namespace Sds.PdfProcessor.Domain.Commands
{
    public class ParseFile : ICorrelatedCommand
    {
        public readonly string Bucket;
        public readonly Guid BlobId;
        public readonly int ByteTypes;

        public ParseFile(Guid id, Guid correlationId, Guid userId, string bucket, Guid blobId, int byteTypes)
        {
            Id = id;
            Bucket = bucket;
            BlobId = blobId;
            CorrelationId = correlationId;
            UserId = userId;
            ByteTypes = byteTypes;
        }

        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public int ExpectedVersion { get; set; }
    }

    [Flags]
    public enum OperationType
    {
        None = 0,
        Text = 1,
        Images = 2,
        Tables = 4
    }
}
