using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Reactions.Sagas.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Users
{
    public class ReactionsCounterEventHandlers : IConsumer<ReactionFileProcessed>
    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Users { get { return _database.GetCollection<BsonDocument>("Users"); } }
        protected IMongoCollection<BsonDocument> Files { get { return _database.GetCollection<BsonDocument>("Files"); } }

        public ReactionsCounterEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        
        public async Task Consume(ConsumeContext<ReactionFileProcessed> context)
        {
            Guid fileId = context.Message.Id;
            var fileView = Files.Find(new BsonDocument("_id", fileId)).FirstOrDefault().AsBsonDocument;
            var userView = Users.Find(new BsonDocument("_id", (Guid)fileView["CreatedBy"])).FirstOrDefault().AsBsonDocument;

            var filter = new BsonDocument("_id", (Guid)userView["_id"]);
            UpdateDefinition<BsonDocument> update;
            update = Builders<BsonDocument>.Update
                    .Inc("Counters.Reactions", context.Message.ProcessedRecords + context.Message.FailedRecords);
            
            await Users.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<BsonDocument>() { IsUpsert = true });
        }
    }
}
