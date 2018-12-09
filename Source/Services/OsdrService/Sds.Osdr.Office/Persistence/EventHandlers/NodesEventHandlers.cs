using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Office.Domain.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Office.Persistence.EventHandlers
{
    public class NodesEventHandlers : IConsumer<OfficeFileCreated>,
                                      IConsumer<PdfBlobUpdated>,
                                      IConsumer<MetadataAdded>

    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Nodes { get { return _database.GetCollection<BsonDocument>("Nodes"); } }

        public NodesEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<PdfBlobUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Pdf.BlobId", context.Message.BlobId)
                .Set("Pdf.Bucket", context.Message.Bucket)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<MetadataAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Properties.Metadata", context.Message.Metadata)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<OfficeFileCreated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);
        }
    }
}
