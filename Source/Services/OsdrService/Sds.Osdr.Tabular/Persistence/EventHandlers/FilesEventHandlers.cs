using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Tabular.Domain.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Tabular.Persistence.EventHandlers
{
    public class FilesEventHandlers : IConsumer<TabularFileCreated>
    {
        private readonly IMongoDatabase database;

        private IMongoCollection<BsonDocument> Files { get { return database.GetCollection<BsonDocument>("Files"); } }

        public FilesEventHandlers(IMongoDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<TabularFileCreated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }
    }
}
