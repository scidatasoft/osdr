using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Domain.Sagas.Events.Files;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Users
{
    public class SpectraCounterEventHandlers : IConsumer<SpectrumFileProcessed>
    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Users { get { return _database.GetCollection<BsonDocument>("Users"); } }
        protected IMongoCollection<BsonDocument> Files { get { return _database.GetCollection<BsonDocument>("Files"); } }

        public SpectraCounterEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        
        public async Task Consume(ConsumeContext<SpectrumFileProcessed> context)
        {
            Guid fileId = context.Message.Id;
            var fileView = Files.Find(new BsonDocument("_id", fileId)).FirstOrDefault().AsBsonDocument;
            var userView = Users.Find(new BsonDocument("_id", (Guid)fileView["CreatedBy"])).FirstOrDefault().AsBsonDocument;
            var filter = new BsonDocument("_id", (Guid)userView["_id"]);
            UpdateDefinition<BsonDocument> update;
            update = Builders<BsonDocument>.Update
                    .Inc("Counters.Spectra", context.Message.ProcessedRecords + context.Message.FailedRecords);
            
            await Users.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<BsonDocument>() { IsUpsert = true });
        }
    }
}
