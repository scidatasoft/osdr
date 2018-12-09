using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Sds.ChemicalStandardizationValidation.Persistance.EventHandlers
{
    public class StandadizationEventHandler : IConsumer<Standardized>,
                                              IConsumer<StandardizationFailed>,
                                              IConsumer<StandardizationDeleted>
    {
        private readonly IMongoDatabase database;
        private readonly string collectionName;
        private readonly IMongoCollection<BsonDocument> collection;

        public StandadizationEventHandler(IMongoDatabase db)
        {
            database = db ?? throw new ArgumentNullException(nameof(db));
            collectionName = "Standardizations";
            collection = database.GetCollection<BsonDocument>(collectionName);
        }

        public async Task Consume(ConsumeContext<Standardized> context)
        {
            Log.Debug($"Saving standardization with id '{context.Message.Id}'");

            await collection.InsertOneAsync(new
            {
                Id = context.Message.Id,
                StandardizedId = context.Message.Record.StandardizedId,
                Issues = context.Message.Record.Issues
            }.ToBsonDocument());

            Log.Debug($"Standardization with id '{context.Message.Id}' saved");
            
            //await eventPublisher.Publish(new StandardizedPersisted(message.Id, message.CorrelationId, message.UserId, message.Record));
        }

        public async Task Consume(ConsumeContext<StandardizationFailed> context)
        {
            var ex = context.Message.Message;
            var storedResult = new 
            {
                Id = context.Message.Id,
                Ex = ex
            }.ToBsonDocument();

            Log.Debug($"Saving exception for failed calculation with id '{context.Message.Id}'");

            await collection.InsertOneAsync(storedResult);

            Log.Debug($"Exception for failed calculation with id '{context.Message.Id}' saved");
        }

        public async Task Consume(ConsumeContext<StandardizationDeleted> context)
        {
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", context.Message.Id));
        }
    }
}
