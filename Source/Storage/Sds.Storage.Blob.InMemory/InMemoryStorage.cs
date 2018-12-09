using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sds.Storage.Blob.InMemory
{
    public class InMemoryStorage : IBlobStorage
    {
        private const string DEFAULT_BUCKET = "memory";

        private IDictionary<string, IDictionary<Guid, IBlob>> buckets = new Dictionary<string, IDictionary<Guid, IBlob>>();

        public async Task<Guid> AddFileAsync(string fileName, Stream source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var id = Guid.NewGuid();

            await AddFileAsync(id, fileName, source, contentType, bucketName, metadata);

            return id;
        }

        public Task AddFileAsync(Guid id, string fileName, Stream source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            if (!buckets.ContainsKey(bucketName ?? DEFAULT_BUCKET))
                buckets[bucketName ?? DEFAULT_BUCKET] = new Dictionary<Guid, IBlob>();

            using (MemoryStream ms = new MemoryStream())
            {
                source.CopyTo(ms);
                buckets[bucketName ?? DEFAULT_BUCKET][id] = new InMemoryBlob(new BlobInfo(id, fileName, contentType, source.Length, "", DateTime.Now, metadata), ms.ToArray());
            }

            return Task.CompletedTask;
        }

        public Task<Guid> AddFileAsync(string fileName, byte[] source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var id = Guid.NewGuid();

            AddFileAsync(id, fileName, source, contentType, bucketName, metadata);

            return Task.FromResult(id);
        }

        public Task AddFileAsync(Guid id, string fileName, byte[] source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            if (!buckets.ContainsKey(bucketName ?? DEFAULT_BUCKET))
                buckets[bucketName ?? DEFAULT_BUCKET] = new Dictionary<Guid, IBlob>();

            buckets[bucketName ?? DEFAULT_BUCKET][id] = new InMemoryBlob(new BlobInfo(id, fileName, contentType, source.Length, "", DateTime.Now, metadata), source);

            return Task.CompletedTask;
        }

        public Task DeleteFileAsync(Guid id, string bucketName = null)
        {
            if (!buckets.ContainsKey(bucketName ?? DEFAULT_BUCKET))
                return Task.CompletedTask;

            if (!buckets[bucketName ?? DEFAULT_BUCKET].ContainsKey(id))
                return Task.CompletedTask;

            buckets[bucketName ?? DEFAULT_BUCKET].Remove(id);

            return Task.CompletedTask;
        }

        public Task DownloadFileToStreamAsync(Guid id, Stream destination, string bucketName = null)
        {
            byte[] blob = buckets[bucketName ?? DEFAULT_BUCKET][id] as InMemoryBlob;
            destination.Write(blob, 0, blob.Length);

            return Task.CompletedTask;
        }

        public Task<IBlob> GetFileAsync(Guid id, string bucketName = null)
        {
            if (!buckets.ContainsKey(bucketName ?? DEFAULT_BUCKET))
                return Task.FromResult(default(IBlob));

            if (!buckets[bucketName ?? DEFAULT_BUCKET].ContainsKey(id))
                return Task.FromResult(default(IBlob));

            return Task.FromResult(buckets[bucketName ?? DEFAULT_BUCKET][id]);
        }

        public Task<IBlobInfo> GetFileInfo(Guid id, string bucketName = null)
        {
            var blob = GetFileAsync(id, bucketName).Result;

            return Task.FromResult(blob.Info);
        }

        public Stream OpenUploadStream(Guid id, string fileName, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var ms = new UploadInMemoryStream(this, id, fileName, contentType, bucketName);

            return ms;

        }

        public Task<Stream> OpenUploadStreamAsync(Guid id, string fileName, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var stream = OpenUploadStream(id, fileName, contentType, bucketName, metadata);

            return Task.FromResult(stream);
        }
    }

    public class UploadInMemoryStream : MemoryStream
    {
        private Guid Id;
        private string FileName;
        private string ContentType;
        private string BucketName;
        private InMemoryStorage Storage;

        public UploadInMemoryStream(InMemoryStorage storage, Guid id, string fileName, string contentType, string bucketName)
        {
            Storage = storage;
            Id = id;
            FileName = fileName;
            ContentType = contentType;
            BucketName = bucketName;
        }

        public async override void Flush()
        {
            await Storage.AddFileAsync(Id, FileName, this, ContentType, BucketName);

            base.Flush();
        }
    }
}