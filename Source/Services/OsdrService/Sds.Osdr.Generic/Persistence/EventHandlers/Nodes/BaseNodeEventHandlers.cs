using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Nodes
{
    public class BaseNodeEventHandlers
    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Nodes { get { return _database.GetCollection<BsonDocument>("Nodes"); } }

        public BaseNodeEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }
    }
}
