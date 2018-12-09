using Sds.Storage.Blob.Core;
using System.IO;

namespace Sds.Storage.Blob.InMemory
{
    public class InMemoryBlob : IBlob
    {
        private byte[] data = null;
        public IBlobInfo Info { get; private set; }

        public InMemoryBlob(IBlobInfo info, byte[] data)
        {
            Info = info;
            this.data = data;
        }

        public void Dispose()
        {
        }

        public Stream GetContentAsStream()
        {
            return new MemoryStream(data);
        }

        public static implicit operator byte[] (InMemoryBlob blob)
        {
            return blob.data;
        }
    }
}
