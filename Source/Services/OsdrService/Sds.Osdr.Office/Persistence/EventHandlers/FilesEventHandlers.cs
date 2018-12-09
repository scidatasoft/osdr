using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Office.Domain.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Office.Persistence.EventHandlers
{
    public class FilesEventHandlers : IConsumer<OfficeFileCreated>,
                                      IConsumer<PdfBlobUpdated>,
                                      IConsumer<MetadataAdded>
    {
        private readonly IMongoDatabase database;

        private IMongoCollection<BsonDocument> Files { get { return database.GetCollection<BsonDocument>("Files"); } }

        public FilesEventHandlers(IMongoDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<PdfBlobUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var file = Files.FindSync(filter);

            var update = Builders<BsonDocument>.Update
                .Set("Pdf.BlobId", context.Message.BlobId)
                .Set("Pdf.Bucket", context.Message.Bucket)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
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

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<OfficeFileCreated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }
    }
}
