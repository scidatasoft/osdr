using MassTransit;
using MongoDB.Driver;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.Imaging.Domain.Models;
using Sds.Storage.Blob.Core;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Imaging.Persistence.CommandHandlers
{
    public class ImagingCommandHandler : IConsumer<DeleteImage>, IConsumer<DeleteSource>
    {
        readonly IBlobStorage _blobStorage;
        readonly IMongoDatabase _database;
        readonly IMongoCollection<FileImages> _imagesMetaCollection;

        public ImagingCommandHandler(IMongoDatabase database, IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _imagesMetaCollection = _database.GetCollection<FileImages>(nameof(FileImages));
        }

        public async Task Consume(ConsumeContext<DeleteSource> context)
        {
            var blobInfo = await _blobStorage.GetFileInfo(context.Message.Id, context.Message.Bucket);

            var doc = await _imagesMetaCollection.FindOneAndDeleteAsync(fd => fd.Id == context.Message.Id);

            Log.Information($"Deleting blob '{context.Message.Id}'.");
            if (!(blobInfo is null))
            {
                await _blobStorage.DeleteFileAsync(context.Message.Id, context.Message.Bucket);

                if (!(doc is null))
                {
                    foreach (var image in doc.Images)
                    {
                        var imageInfo = await _blobStorage.GetFileInfo(image.Id, context.Message.Bucket);
                        if (!(imageInfo is null))
                        {
                            Log.Information($"Deleting image '{image.Id}'.");
                            await _blobStorage.DeleteFileAsync(image.Id, context.Message.Bucket);
                        }
                    }
                }

                await context.Publish<SourceDeleted>(new
                {
                    Id = context.Message.Id,
                    Bucket = context.Message.Bucket,
                    CorrelationId = context.Message.CorrelationId,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
        }

        public async Task Consume(ConsumeContext<DeleteImage> context)
        {
            var blobInfo = await _blobStorage.GetFileInfo(context.Message.Id, context.Message.Bucket);
            if (!(blobInfo is null))
            {
                Log.Information($"Deleting image '{context.Message.Id}'.");

                await _blobStorage.DeleteFileAsync(context.Message.Id, context.Message.Bucket);

                await context.Publish<ImageDeleted>(new
                {
                    Id = context.Message.Id,
                    Bucket = context.Message.Bucket,
                    CorrelationId = context.Message.CorrelationId,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
            var update = Builders<FileImages>.Update.PullFilter(fi => fi.Images, f => f.Id == context.Message.Id);
            await _imagesMetaCollection.FindOneAndUpdateAsync(fi => fi.Images.Any(i => i.Id == context.Message.Id), update);
        }
    }
}
