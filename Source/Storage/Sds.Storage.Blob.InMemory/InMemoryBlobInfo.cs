using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;

namespace Sds.Storage.Blob.InMemory
{
    public class BlobInfo : IBlobInfo
    {
        public Guid Id { get; private set; }
        public string FileName { get; private set; }
        public string ContentType { get; private set; }
        public long Length { get; private set; }
        public IDictionary<string, object> Metadata { get; private set; }
        public string MD5 { get; private set; }
        public DateTime UploadDateTime { get; private set; }

        public BlobInfo(Guid id, string fileName, string contentType, long length, string md5, DateTime uploadDate, IDictionary<string, object> metadata = null)
        {
            Id = id;
            FileName = fileName;
            ContentType = contentType;
            Length = length;
            MD5 = md5;
            UploadDateTime = uploadDate;
            Metadata = metadata;
        }
    }
}
