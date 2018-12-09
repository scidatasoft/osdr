using System;
using System.Collections.Generic;

namespace Sds.Storage.Blob.Domain.Dto
{
    public class LoadedBlobInfo
    {
        public readonly Guid Id;
        public readonly string Bucket;
        public readonly IDictionary<string, object> Metadata;
        public readonly Guid? UserId;
        public readonly string FileName;
        public readonly string MD5;
        public readonly DateTime UploadDateTime;
        public readonly long Length;

        public LoadedBlobInfo(Guid id, string fileName, long length, Guid? userId, DateTime uploadDateTime, string md5, string bucket = null, IDictionary<string, object> metadata = null)
        {
            Id = id;
            UserId = userId;
            Bucket = bucket;
            FileName = fileName;
            UploadDateTime = uploadDateTime;
            MD5 = md5;
            Length = length;
            Metadata = metadata;
        }
	}
}
