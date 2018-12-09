using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.ChemicalProperties.Domain.Commands;
using Sds.ChemicalProperties.Domain.Events;
using System;
using System.Threading.Tasks;

namespace Sds.ChemicalProperties.Persistence.CommandHandlers
{
    public class ChemicalPropertiesCommandHandler : IConsumer<DeleteChemicalProperties>
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;

        public ChemicalPropertiesCommandHandler(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _collection = _database.GetCollection<BsonDocument>(nameof(ChemicalProperties));
        }

        public async Task Consume(ConsumeContext<DeleteChemicalProperties> context)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", context.Message.Id);
            await _collection.DeleteOneAsync(filter);

            await context.Publish<ChemicalPropertiesDeleted>(new
            {
                Id = context.Message.Id,
                CorrelationId = context.Message.CorrelationId,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }
    }
}
