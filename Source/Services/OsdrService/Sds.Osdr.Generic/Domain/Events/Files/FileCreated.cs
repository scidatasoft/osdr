using CQRSlite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class FileCreated : IEvent
    {
        public string Bucket { get; set; }
        public Guid BlobId { get; set; }
        public Guid? ParentId { get; set; }
        public string FileName { get; set; }
        public long Length { get; set; }
        public string Md5 { get; set; }
        public FileStatus FileStatus { get; set; }
        public FileType FileType { get; set; }

        public FileCreated(Guid id, string bucket, FileStatus fileStatus, Guid blobId, Guid userId, Guid? parentId, string fileName, long length, string md5, FileType type)
		{
			Id = id;
            Bucket = bucket;
            BlobId = blobId;
            UserId = userId;
            ParentId = parentId;
            FileName = fileName;
			FileStatus = fileStatus;
			Length = length;
            Md5 = md5;
            FileType = type;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
	}
}
