using System.Threading.Tasks;
using System.IO;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using Sds.Storage.Blob.Core;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Sds.Storage.Blob.GridFs
{
    public class GridFsStorage : IBlobStorage
    {
        private IMongoDatabase _database;

        public GridFsStorage(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public GridFsStorage(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<Guid> AddFileAsync(string fileName, Stream source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var id = Guid.NewGuid();

            await AddFileAsync(id, fileName, source, contentType, bucketName, metadata);

            return id;
        }

        public async Task AddFileAsync(Guid id, string fileName, byte[] source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var bucket = GetBucket(bucketName);
            var bsonMetadata = new BsonDocument { { nameof(IBlobInfo.ContentType), contentType } };

            if (metadata != null)
            {
                bsonMetadata.AddRange(metadata);
            }

            await bucket.UploadFromBytesAsync(id, fileName, source, new GridFSUploadOptions { Metadata = bsonMetadata });
        }

        public async Task<Guid> AddFileAsync(string fileName, byte[] source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var id = Guid.NewGuid();

            await AddFileAsync(id, fileName, source, contentType, bucketName, metadata);

            return id;
        }

        public async Task AddFileAsync(Guid id, string fileName, Stream source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var bucket = GetBucket(bucketName);
            var bsonMetadata = new BsonDocument { { nameof(IBlobInfo.ContentType), contentType } };

            if (metadata!=null)
            {
                bsonMetadata.AddRange(metadata);
            }
            
            await bucket.UploadFromStreamAsync(id, fileName, source, new GridFSUploadOptions { Metadata = bsonMetadata });
        }

        public async Task DeleteFileAsync(Guid id, string bucketName = null)
        {
            var bucket = GetBucket(bucketName);
            await bucket.DeleteAsync(id);
        }

        public async Task<IBlob> GetFileAsync(Guid id, string bucketName = null)
        {
            var bucket = GetBucket(bucketName);
            var stream = await bucket.OpenDownloadStreamAsync(id);

            return new GridFsBlob(stream);
        }

        public async Task<IBlobInfo> GetFileInfo(Guid id, string bucketName = null)
        {
            var bucket = GetBucket(bucketName);
            var filter = Builders<GridFSFileInfo<Guid>>.Filter.Eq(f => f.Id, id);
            using (var cursor = await bucket.FindAsync(filter))
            {
                var fileInfo = await cursor.FirstOrDefaultAsync();
                return fileInfo != null ? new GridFsBlobInfo(fileInfo) : null;  // fileInfo either has the matching file information or is null
            }
        }
        public async Task DownloadFileToStreamAsync(Guid id, Stream destination, string bucketName = null)
        {
            var bucket = GetBucket(bucketName);
            await bucket.DownloadToStreamAsync(id, destination);
        }

        private GridFSBucket<Guid> GetBucket(string bucketName)
        {
            return string.IsNullOrEmpty(bucketName)
                            ? new GridFSBucket<Guid>(_database)
                            : new GridFSBucket<Guid>(_database, new GridFSBucketOptions
                            {
                                BucketName = bucketName,
                                //WriteConcern = WriteConcern.Unacknowledged // !! prevent stuck in docker container under high loads
                            });
        }

        public Stream OpenUploadStream(Guid id, string fileName, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var bucket = GetBucket(bucketName);
            var bsonMetadata = new BsonDocument { { nameof(IBlobInfo.ContentType), contentType } };

            if (metadata != null)
            {
                bsonMetadata.AddRange(metadata);
            }

            return bucket.OpenUploadStream(id, fileName, new GridFSUploadOptions { Metadata = bsonMetadata });
        }

        public async Task<Stream> OpenUploadStreamAsync(Guid id, string fileName, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null)
        {
            var bucket = GetBucket(bucketName);
            var bsonMetadata = new BsonDocument { { nameof(IBlobInfo.ContentType), contentType } };

            if (metadata != null)
            {
                bsonMetadata.AddRange(metadata);
            }

            return await bucket.OpenUploadStreamAsync(id, fileName, new GridFSUploadOptions { Metadata = bsonMetadata });
        }
    }
}
