using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sds.Storage.Blob.Core
{
    public static class BlobStorageExtensions
    {
        public static async Task<Guid> AddFileAsync(this IBlobStorage storage, string fileName, byte[] source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            return await storage.AddFileAsync(fileName, new MemoryStream(source), contentType, bucketName, metadata);
        }

        public static async Task AddFileAsync(this IBlobStorage storage, Guid id, string fileName, byte[] source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            await storage.AddFileAsync(id, fileName, new MemoryStream(source), contentType, bucketName, metadata);
        }
    }
}
