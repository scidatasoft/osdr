using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sds.ChemicalStandardizationValidation.Persistance.EventHandlers
{
    public class ValidationEventHandler : IConsumer<Validated>,
                                            IConsumer<ValidationFailed>,
                                            IConsumer<ValidationDeleted>
    {
        private readonly IMongoDatabase database;
        private readonly string collectionName;
        private readonly IMongoCollection<BsonDocument> collection;

        public ValidationEventHandler(IMongoDatabase db)
        {
            database = db ?? throw new ArgumentNullException(nameof(db));
            collectionName = "Validations";
            collection = database.GetCollection<BsonDocument>(collectionName);
        }

        public async Task Consume(ConsumeContext<Validated> context)
        {
            Log.Debug($"Saving validation with id '{context.Message.Id}'");

            await collection.InsertOneAsync(new
            {
                Id = context.Message.Id,
                Issues = context.Message.Record.Issues
            }.ToBsonDocument());

            Log.Debug($"Validation with id '{context.Message.Id}' saved");

            //await eventPublisher.Publish(new ValidatedPersisted(message.Id, message.CorrelationId, message.UserId, message.Record));
        }

        public async Task Consume(ConsumeContext<ValidationFailed> context)
        {
            var col = database.GetCollection<BsonDocument>(collectionName);
            var storedResult = new 
            {
                Id = context.Message.Id,
                Ex = context.Message.Message
            }.ToBsonDocument();

            await col.InsertOneAsync(storedResult);
        }

        public async Task Consume(ConsumeContext<ValidationDeleted> context)
        {
            var col = database.GetCollection<BsonDocument>(collectionName);
            await col.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", context.Message.Id));
        }
    }
}
