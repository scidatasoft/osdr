using Sds.CqrsLite.Events;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class RecordCreated : IUserEvent
    {
        public readonly string Bucket;
        public readonly Guid BlobId;
        public RecordType RecordType { get; set; }
        public readonly Guid FileId;
        public readonly long Index;
        public readonly IEnumerable<Field> Fields;

        public RecordCreated(Guid id, string bucket, Guid blobId, Guid userId, RecordType recordType, Guid fileId, long index, IEnumerable<Field> fields = null)
        {
            Id = id;
            Bucket = bucket;
            BlobId = blobId;
            UserId = userId;
            RecordType = recordType;
            FileId = fileId;
            Index = index;
            Fields = fields;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
