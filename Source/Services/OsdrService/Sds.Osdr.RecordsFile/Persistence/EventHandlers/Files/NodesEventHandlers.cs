using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Events.Files;
using Sds.Osdr.Generic.Domain.Events.Nodes;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using Sds.Osdr.RecordsFile.Domain.Events.Files;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.Persistence.EventHandlers.Files
{
    public class NodesEventHandlers : IConsumer<FieldsAdded>,
                                      IConsumer<RecordsFileCreated>,
                                      IConsumer<TotalRecordsUpdated>,
                                      IConsumer<AggregatedPropertiesAdded>,
                                      IConsumer<FileDeleted>
    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Nodes { get { return _database.GetCollection<BsonDocument>("Nodes"); } }

        public NodesEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<RecordsFileCreated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<FieldsAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<TotalRecordsUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("TotalRecords", context.Message.TotalRecords)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new NodePersisted<TotalRecordsUpdated>(context.Message, context.Message.Id, context.Message.UserId, NodeType.File.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }

        public async Task Consume(ConsumeContext<AggregatedPropertiesAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<FileDeleted> context)
        {
            var childNodes = await Nodes.Find(new BsonDocument("ParentId", context.Message.Id).Add("IsDeleted", new BsonDocument("$ne", true)))
                .Project("{Type:1, Version:1}")
                .ToListAsync();

            foreach (var record in childNodes.Where(n => string.Equals(n["Type"].AsString, "Record", StringComparison.OrdinalIgnoreCase)))
            {
                await context.Publish<DeleteRecord>(new
                {
                    Id = record["_id"].AsGuid,
                    UserId = context.Message.UserId,
                    ExpectedVersion = record["Version"].AsInt32,
                    Force = context.Message.Force
                });
            }
        }
    }
}
