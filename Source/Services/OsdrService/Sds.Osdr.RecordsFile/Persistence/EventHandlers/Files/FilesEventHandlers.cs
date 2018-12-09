using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.RecordsFile.Domain.Events.Files;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.Persistence.EventHandlers.Files
{
    public class FilesEventHandlers : IConsumer<RecordsFileCreated>,
                                      IConsumer<FieldsAdded>,
                                      IConsumer<TotalRecordsUpdated>,
                                      IConsumer<AggregatedPropertiesAdded>
    {
        private readonly IMongoDatabase database;

        private IMongoCollection<BsonDocument> Files { get { return database.GetCollection<BsonDocument>("Files"); } }

        public FilesEventHandlers(IMongoDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<RecordsFileCreated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<FieldsAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Properties.Fields", context.Message.Fields)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<AddedFieldsPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                Fields = context.Message.Fields
            });
        }

        public async Task Consume(ConsumeContext<TotalRecordsUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("TotalRecords", context.Message.TotalRecords)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<TotalRecordsPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                TotalRecords = context.Message.TotalRecords
            });
        }

        public async Task Consume(ConsumeContext<AggregatedPropertiesAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Properties.ChemicalProperties", context.Message.Properties)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }
    }
}
