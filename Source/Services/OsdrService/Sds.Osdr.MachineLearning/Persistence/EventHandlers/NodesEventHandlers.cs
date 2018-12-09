using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Commands.Files;
using Sds.Osdr.Generic.Domain.Events.Nodes;
using Sds.Osdr.Generic.Extensions;
using Sds.Osdr.MachineLearning.Domain.Events;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.Persistence.EventHandlers
{
    public class NodesEventHandlers : IConsumer<ModelCreated>,
                                      IConsumer<ModelPropertiesUpdated>,
                                      IConsumer<StatusChanged>,
                                      IConsumer<ImageAdded>,
                                      IConsumer<ModelNameUpdated>,
                                      IConsumer<ModelDeleted>,
                                      IConsumer<ModelMoved>,
                                      IConsumer<TargetsChanged>,
                                      IConsumer<PermissionsChanged>,
                                      IConsumer<ConsensusWeightChanged>,
                                      IConsumer<ModelBlobChanged>

    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Nodes { get { return _database.GetCollection<BsonDocument>("Nodes"); } }


        public NodesEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<ModelCreated> context)
        {

            var document = new BsonDocument("_id", context.Message.Id)
                .Set("Type", NodeType.Model.ToString())
                .Set("Status", context.Message.Status.ToString())
                .Set("OwnedBy", context.Message.UserId)
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("ParentId", context.Message.ParentId)
                .Set("Version", context.Message.Version);

            if (context.Message.BlobId != null && context.Message.Bucket != null)
            {
                document = document.Set("Blob", new { _id = context.Message.BlobId ?? Guid.Empty, Bucket = context.Message.Bucket }.ToBsonDocument());
            }

            if (context.Message.Name != null)
            {
                document = document.Set("Name", context.Message.Name);
            }

            await Nodes.InsertOneAsync(document);

            await context.Publish<ModelPersisted>(new
            {
                Id = context.Message.Id,
                Method = context.Message.Method,
                UserId = context.Message.UserId
            });
        }

        public async Task Consume(ConsumeContext<ModelPropertiesUpdated> context)
        {
            var message = context.Message;
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<Domain.Events.StatusChanged> context)
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

            Log.Debug($"GenericFile Persistance: NodeStatusPersisted({context.Message.Status}) {context.Message.Id}");

            await context.Publish<ModelStatusPersisted>(new
            {
                context.Message.Id,
                Status = context.Message.Status,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                ParentId = node["ParentId"].AsGuid,
                context.Message.Version
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

        }

        public async Task Consume(ConsumeContext<ModelNameUpdated> context)
        {
            var message = context.Message;
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Name", context.Message.Name)
                .Set("Version", context.Message.Version);

            var document = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<ModelPersisted>(new
            {
                message.Id,
                FileName = context.Message.Name,
                message.UserId
            });
        }

        public async Task Consume(ConsumeContext<ModelDeleted> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id);
            if(!context.Message.Force)
            {
                filter.Add("Version", context.Message.Version - 1);
            }

            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("IsDeleted", true)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            var insideFilesNodes = await Nodes.Find(new BsonDocument("ParentId", context.Message.Id).Add("IsDeleted", new BsonDocument("$ne", true)))
                .Project("{Version:1}")
                .ToListAsync();

            foreach (var fileNode in insideFilesNodes)
            {
                await context.Publish<DeleteFile>(new
                {
                    Id = fileNode["_id"].AsGuid,
                    UserId = context.Message.UserId,
                    ExpectedVersion = fileNode["Version"].AsInt32,
                    Force = context.Message.Force
                });
            }
        }

        public async Task Consume(ConsumeContext<ModelMoved> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("ParentId", context.Message.NewParentId)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var document = await Nodes.FindOneAndUpdateAsync(filter, Builders<BsonDocument>.Update.Set("Version", context.Message.Version));

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<Domain.Events.PermissionChangedPersisted>(new
            {
                context.Message.Id,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                ParentId = document["ParentId"].AsGuid,
                context.Message.AccessPermissions,
            });
        }

        public async Task Consume(ConsumeContext<TargetsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Targets", context.Message.Targets)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("Version", context.Message.Version);

            var document = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<ConsensusWeightChanged> context)
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

        public async Task Consume(ConsumeContext<ModelBlobChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
                .Set("Blob", new { _id = context.Message.BlobId, Bucket = context.Message.Bucket }.ToBsonDocument())
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }
    }
}
