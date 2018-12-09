using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.ChemicalProperties.Domain.Events;
using System;
using System.Threading.Tasks;
using Serilog;

namespace Sds.ChemicalProperties.Persistence.EventHandler
{
    public class ChemicalPropertiesEventHandler : IConsumer<ChemicalPropertiesCalculated>, IConsumer<ChemicalPropertiesCalculationFailed>
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;

        public ChemicalPropertiesEventHandler(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _collection = _database.GetCollection<BsonDocument>(nameof(ChemicalProperties));
        }

        public async Task Consume(ConsumeContext<ChemicalPropertiesCalculated> context)
        {
            var document = new
            {
                Id = context.Message.Id,
                CalculationDate = DateTime.Now,
                PropertiesCalculationResult = context.Message.Result
            }.ToBsonDocument();

            Log.Information($"Saving calculated properties with id '{context.Message.Id}'");

            await _collection.InsertOneAsync(document);

            Log.Information($"Calculated properties with id '{context.Message.Id}' saved");

            await context.Publish<ChemicalPropertiesCalculationPersisted>(new
            {
                Id = context.Message.Id,
                CorrelationId = context.Message.CorrelationId,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<ChemicalPropertiesCalculationFailed> context)
        {
            var document = new
            {
                Id = context.Message.Id,
                CalculationDate = DateTime.Now,
                Exception = context.Message.CalculationException
            }.ToBsonDocument();

            Log.Information($"Saving exception for failed calculation with id '{context.Message.Id}'");

            await _collection.InsertOneAsync(document);

            Log.Information($"Exception for failed calculation with id '{context.Message.Id}' saved");
        }
    }
}
