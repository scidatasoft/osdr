using MassTransit;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.Domain.Dto;
using Sds.Storage.Blob.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class OsdrService
    {
        protected readonly IBlobStorage _blobStorage;

        public IBlobStorage BlobStorage { get { return _blobStorage; } }

        public OsdrService(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        protected async Task<Guid> LoadBlob(IPublishEndpoint endpoint, Guid userId, string bucket, string fileName, string contentType = "application/octet-stream", IDictionary<string, object> metadata = null)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);

            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            var blobId = await _blobStorage.AddFileAsync(fileName, File.OpenRead(path), contentType, bucket, metadata);

            var blobInfo = await _blobStorage.GetFileInfo(blobId, bucket);

            await endpoint.Publish<BlobLoaded>(new
            {
                //CorrelationId = blobInfo.Metadata != null ? blobInfo.Metadata.ContainsKey("correlationId") ? new Guid(blobInfo.Metadata["correlationId"].ToString()) : Guid.Empty,
                BlobInfo = new LoadedBlobInfo(blobId, fileName, blobInfo.Length, userId, blobInfo.UploadDateTime, blobInfo.MD5, bucket, blobInfo.Metadata),
                TimeStamp = DateTimeOffset.UtcNow
            });

            //Thread.Sleep(100);

            Log.Debug($"BlobLoaded: {fileName}; BlobId: {blobId}");

            return blobId;
        }
    }
}
