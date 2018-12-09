using MassTransit;
using MongoDB.Driver;
using Sds.Imaging.Domain.Events;
using Sds.Imaging.Domain.Models;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Imaging.Persistence.EventHandlers
{
    public class ImagingEventHandler : IConsumer<ImageGenerationFailed>, IConsumer<ImageGenerated>
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<FileImages> _imagesMetaCollection;

        public ImagingEventHandler(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _imagesMetaCollection = _database.GetCollection<FileImages>(nameof(FileImages));
            //_eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public async Task Consume(ConsumeContext<ImageGenerationFailed> context)
        {
            var update = Builders<FileImages>.Update.Set(fi => fi.Images[-1].Exception, context.Message.Image.Exception);
            await _imagesMetaCollection.FindOneAndUpdateAsync(fi => fi.Id == context.Message.Id && fi.Images.Any(i => i.Id == context.Message.Image.Id), update);
        }

        public async Task Consume(ConsumeContext<ImageGenerated> context)
        {
            var update = Builders<FileImages>.Update
            .SetOnInsert(fi => fi.Bucket, context.Message.Bucket)
            .AddToSet(fi => fi.Images, context.Message.Image);

            try
            {
                await _imagesMetaCollection.FindOneAndUpdateAsync<FileImages>(fd => fd.Id == context.Message.BlobId, update, new FindOneAndUpdateOptions<FileImages> { IsUpsert = true });
            }
            catch (MongoCommandException e)
            {
                if (e.Code != 11000) //duplicate key
                    throw e;
                
                Log.Error($"Error persisting image info for file '{context.Message.BlobId}' in bucket '{context.Message.Bucket}', image: '{context.Message.Image.Id}' format: '{context.Message.Image.Format}' with exception {e}");
            }
        }
    }
}
