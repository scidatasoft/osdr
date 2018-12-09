using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.WebPage.Domain.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.WebPage.Persistence.EventHandlers
{
    public class FilesEventHandlers : IConsumer<WebPageCreated>,
                                      IConsumer<WebPageUpdated>
    {
        private readonly IMongoDatabase database;

        private IMongoCollection<BsonDocument> Files { get { return database.GetCollection<BsonDocument>("Files"); } }

        public FilesEventHandlers(IMongoDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<WebPageCreated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Url", context.Message.Url)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<WebPageUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Json.BlobId", context.Message.JsonBlobId)
                .Set("Json.Bucket", context.Message.Bucket)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Files.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);
        }
    }
}
