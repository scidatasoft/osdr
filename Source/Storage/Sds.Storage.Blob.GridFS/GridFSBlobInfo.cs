using MongoDB.Driver.GridFS;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;

namespace Sds.Storage.Blob.GridFs
{
    public class GridFsBlobInfo : IBlobInfo
    {
        private readonly GridFSFileInfo<Guid> _fileInfo;

        public string ContentType
        {
            get { return _fileInfo.Metadata[nameof(IBlobInfo.ContentType)].ToString(); }
        }
        public string FileName
        {
            get { return _fileInfo.Filename; }
        }
        public IDictionary<string, object> Metadata
        {
            get { return _fileInfo.Metadata.ToDictionary(); }
        }
        public Guid Id
        {
            get { return _fileInfo.Id; }
        }

        public long Length
        {
            get { return _fileInfo.Length; }
        }
        public string MD5
        {
            get { return _fileInfo.MD5; }
        }
        public DateTime UploadDateTime
        {
            get { return _fileInfo.UploadDateTime; }
        }

        public GridFsBlobInfo(GridFSFileInfo<Guid> fileInfo)
        {
            _fileInfo = fileInfo;
        }
    }
}
