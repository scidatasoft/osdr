using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Events.Nodes;
using Sds.Osdr.Generic.Extensions;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.Persistence.EventHandlers.Records
{
    public class NodesEventHandlers : IConsumer<RecordCreated>,
                                      IConsumer<StatusChanged>,
                                      IConsumer<ImageAdded>,
                                      IConsumer<IssueAdded>,
                                      IConsumer<IssuesAdded>,
                                      IConsumer<PropertiesAdded>,
                                      IConsumer<RecordDeleted>,
                                      IConsumer<FieldsUpdated>,
									  IConsumer<InvalidRecordCreated>,
                                      IConsumer<PermissionsChanged>
    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Nodes { get { return _database.GetCollection<BsonDocument>("Nodes"); } }

        public NodesEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<RecordCreated> context)
        {
            var document = new BsonDocument("_id", context.Message.Id)
                .Set("Type", NodeType.Record.ToString())
                .Set("SubType", context.Message.RecordType.ToString())
                .Set("Name", context.Message.Index.ToString())
                .Set("Blob", (new { context.Message.Bucket, Id = context.Message.BlobId }).ToBsonDocument())
                .Set("Index", context.Message.Index)
                .Set("OwnedBy", context.Message.UserId)
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("ParentId", context.Message.FileId)
                .Set("Version", context.Message.Version);

            await Nodes.InsertOneAsync(document);

            await context.Publish(new NodePersisted<RecordCreated>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Record.ToString(), context.Message.FileId));
        }

        public async Task Consume(ConsumeContext<StatusChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Status", context.Message.Status.ToString())
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(
                new NodePersisted<StatusChanged>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Record.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));

            await context.Publish<NodeStatusPersisted>(new
            {
                Id = context.Message.Id,
                Status = context.Message.Status,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<ImageAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Push("Images", new
                {
                    context.Message.Image.Id,
                    context.Message.Image.Height,
                    context.Message.Image.Width,
                    context.Message.Image.MimeType,
                    Scale = context.Message.Image.GetScale(),
                    context.Message.Image.Bucket
                })
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new NodePersisted<ImageAdded>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Record.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }

        public async Task Consume(ConsumeContext<IssueAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new NodePersisted<IssueAdded>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Record.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }

        public async Task Consume(ConsumeContext<IssuesAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new NodePersisted<IssuesAdded>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Record.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }

        public async Task Consume(ConsumeContext<PropertiesAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new NodePersisted<PropertiesAdded>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Record.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }

        public async Task Consume(ConsumeContext<RecordDeleted> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id);
            if (!context.Message.Force)
                filter.Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
                .Set("IsDeleted", true)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new NodePersisted<RecordDeleted>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Record.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }

        public async Task Consume(ConsumeContext<FieldsUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Properties.Fields", context.Message.Fields)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new NodePersisted<FieldsUpdated>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Record.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }
		public async Task Consume(ConsumeContext<InvalidRecordCreated> context)
		{
			var document = new BsonDocument("_id", context.Message.Id)
                .Set("Type", NodeType.Record.ToString())
                .Set("SubType", context.Message.RecordType.ToString())
                .Set("Name", context.Message.Index.ToString())
                .Set("ParentId", context.Message.FileId)
				.Set("OwnedBy", context.Message.UserId)
				.Set("CreatedBy", context.Message.UserId)
				.Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
				.Set("UpdatedBy", context.Message.UserId)
				.Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
				.Set("Index", context.Message.Index)
                .Set("Status", RecordStatus.Failed.ToString())
                .Set("Message", context.Message.Error)
				.Set("Version", context.Message.Version);

			await Nodes.InsertOneAsync(document);

			await context.Publish(new NodePersisted<InvalidRecordCreated>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Record.ToString(), context.Message.FileId));

            await context.Publish<NodeRecordPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var document = await Nodes.FindOneAndUpdateAsync(filter, Builders<BsonDocument>.Update.Set("Version", context.Message.Version));

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<NodePermissionChangedPersisted>(new
            {
                context.Message.Id,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                ParentId = document["ParentId"].AsGuid,
                context.Message.AccessPermissions,
            });
        }
    }
}
