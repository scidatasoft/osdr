using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Users
{
    public class RecordDeletedEventHandler : IConsumer<RecordDeleted>
    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Users { get { return _database.GetCollection<BsonDocument>("Users"); } }
        
        public RecordDeletedEventHandler(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }
                
        public async Task Consume(ConsumeContext<RecordDeleted> context)
        {
            var userId = context.Message.UserId;
            var userView = Users.Find(new BsonDocument("_id", userId)).FirstOrDefault().AsBsonDocument;
            BsonValue counter;
            string counterName = GetCounterName(context.Message.RecordType);
            try
            {
                counter = userView["Counters"][counterName];
            }
            catch
            {
                counter = null;
            }

            var filter = new BsonDocument("_id", userId);
            UpdateDefinition<BsonDocument> update;
            if (counter == null || counter <= 0)
            {
                update = Builders<BsonDocument>.Update
                    .Set($"Counters.{counterName}", 0);
            }
            else
            {
                update = Builders<BsonDocument>.Update
                    .Inc($"Counters.{counterName}", -1);
            }

            await Users.FindOneAndUpdateAsync(filter, update);
        }

        private string GetCounterName(RecordType recordType)
        {
            switch(recordType)
            {
                case RecordType.Spectrum:
                    return "Spectra";
                    
                default:
                    return recordType.ToString() + "s";
            }
        }
    }
}
