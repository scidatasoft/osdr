using System.IO;
using MongoDB.Driver.GridFS;
using Sds.Storage.Blob.Core;
using System;

namespace Sds.Storage.Blob.GridFs
{
    public class GridFsBlob : IBlob
    {
        private GridFSDownloadStream<Guid> _stream;
        private GridFsBlobInfo _fileInfo;

        public GridFsBlob(GridFSDownloadStream<Guid> stream)
        {
            _stream = stream;
            _fileInfo = new GridFsBlobInfo(stream.FileInfo);
        }

        public Stream GetContentAsStream()
        {
            return _stream; 
        }

        public IBlobInfo Info
        {
            get { return _fileInfo; }
        }

#region IDisposable
        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _stream.Dispose();
            }
           
            disposed = true;
        }
#endregion
    }
}
