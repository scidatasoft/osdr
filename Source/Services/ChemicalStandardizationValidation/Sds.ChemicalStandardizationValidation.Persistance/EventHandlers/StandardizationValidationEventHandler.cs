using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Sds.ChemicalStandardizationValidation.Persistance.EventHandlers
{
    public class StandardizationValidationEventHandler : IConsumer<ValidatedStandardized>,
                                                         IConsumer<StandardizationValidationDeleted>,
                                                         IConsumer<StandardizationValidationFailed>
    {
        private readonly IMongoDatabase database;
        private readonly string collectionName;
        private readonly IMongoCollection<BsonDocument> collection;

        public StandardizationValidationEventHandler(IMongoDatabase db)
        {
            database = db ?? throw new ArgumentNullException(nameof(db));
            collectionName = "StandardizationsValidations";
            collection = database.GetCollection<BsonDocument>(collectionName);
        }

        public async Task Consume(ConsumeContext<ValidatedStandardized> context)
        {
            var document = new
            {
                Id = context.Message.Id,
                Issues = context.Message.Record.Issues,
                StandardizedId = context.Message.Record.StandardizedId

            }.ToBsonDocument();

            Log.Debug($"Saving StandardizationValidation with id '{context.Message.Id}'");

            await collection.InsertOneAsync(document);

            Log.Debug($"StandardizationValidation with id '{context.Message.Id}' saved");

            //await eventPublisher.Publish(new ValidatedStandardizedPersisted(message.Id, message.CorrelationId, message.UserId, message.Record));
        }

        public async Task Consume(ConsumeContext<StandardizationValidationDeleted> context)
        {
            var col = database.GetCollection<BsonDocument>(collectionName);
            await col.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", context.Message.Id));
        }

        public async Task Consume(ConsumeContext<StandardizationValidationFailed> context)
        {
            var storedResult = new
            {
                Id = context.Message.Id,
                Ex = context.Message.Message
            }.ToBsonDocument();

            await collection.InsertOneAsync(BsonDocument.Create(storedResult));
        }
    }
}
