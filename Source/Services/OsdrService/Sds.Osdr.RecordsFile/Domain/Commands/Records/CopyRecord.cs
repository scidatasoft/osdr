using Sds.CqrsLite.Commands;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Records
{
    public class CopyRecord : IUserCommand
    {
		public Guid SourceId { get; set; }
		public Guid FileId { get; set; }
		public Guid BlobId { get; set; }
		public string Bucket { get; set; }
		public long Index { get; set; }

        public CopyRecord(Guid sourceId, Guid id, Guid fileId, Guid userId, Guid blobId, string bucket, long index)
        {
			SourceId = sourceId;
			Id = id;
            UserId = userId;
			FileId = fileId;
			BlobId = blobId;
			Bucket = bucket;
			Index = index;
		}

        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int ExpectedVersion { get; set; }
	}
}
