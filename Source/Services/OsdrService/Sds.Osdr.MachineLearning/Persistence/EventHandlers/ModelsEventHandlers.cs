using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Sds.Osdr.Generic.Extensions;
using Sds.Osdr.MachineLearning.Domain.Events;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.Persistence.EventHandlers
{
    public class ModelsEventHandlers : IConsumer<ModelCreated>,
                                       IConsumer<ModelPropertiesUpdated>,
                                       IConsumer<ImageAdded>,
                                       IConsumer<StatusChanged>,
                                       IConsumer<ModelNameUpdated>,
                                       IConsumer<ModelDeleted>,
                                       IConsumer<ModelMoved>,
                                       IConsumer<PermissionsChanged>,
                                       IConsumer<TargetsChanged>,
                                       IConsumer<ConsensusWeightChanged>,
                                       IConsumer<ModelBlobChanged>

    {
        private readonly IMongoDatabase database;

        private IMongoCollection<BsonDocument> Models { get { return database.GetCollection<BsonDocument>("Models"); } }
        private IMongoCollection<BsonDocument> AccessPermissions => database.GetCollection<BsonDocument>("AccessPermissions");

        public ModelsEventHandlers(IMongoDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<ModelCreated> context)
        {

            var message = context.Message;

            var document = new BsonDocument("_id", context.Message.Id)
                .Set("OwnedBy", context.Message.UserId)
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("KFold", context.Message.KFold)
                .Set("TestDatasetSize", context.Message.TestDatasetSize)
                .Set("SubSampleSize", context.Message.SubSampleSize)
                .Set("ClassName", context.Message.ClassName)
                .Set("Fingerprints", new BsonArray(context.Message.Fingerprints.Select(f => new BsonDocument {
                        { "Type", f.Type },
                        { "Size", f.Size },
                        { "Radius", f.Radius }
                })))
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("ParentId", context.Message.ParentId)
                .Set("Status", context.Message.Status.ToString())
                .Set("Method", context.Message.Method)
                .Set("Scaler", context.Message.Scaler)
                .Set("Version", context.Message.Version);

            if (message.DisplayMethodName != null)
            {
                document = document.Set("DisplayMethodName", message.DisplayMethodName);
            }

            if (message.BlobId != null && message.Bucket != null)
            {
                document = document.Set("Blob", new BsonDocument("_id", context.Message.BlobId).Add("Bucket", message.Bucket));
            }

            if (context.Message.Name != null)
            {
                document = document.Set("Name", context.Message.Name);
            }

            if (context.Message.Dataset != null)
            {
                var jsonDataset = JsonConvert.SerializeObject(message.Dataset);
                var modelDatasetBson = BsonSerializer.Deserialize<BsonDocument>(jsonDataset);
                modelDatasetBson.Remove("BlobId");
                modelDatasetBson.Add("BlobId", message.Dataset.BlobId);
                document = document.Set("Dataset", modelDatasetBson);
            }

            if (context.Message.Property != null)
            {
                document = document.Set("Property", message.Property.ToBsonDocument());
            }

            if (message.Metadata != null)
            {
                var jsonMeta = JsonConvert.SerializeObject(message.Metadata);
                var modelMetaBson = BsonSerializer.Deserialize<BsonDocument>(jsonMeta);
                document = document.Set("Metadata", modelMetaBson);
            }

            await Models.InsertOneAsync(document);

            await context.Publish<ModelPersisted>(new
            {
                context.Message.Id,
                context.Message.UserId,
                context.Message.BlobId,
                context.Message.Bucket,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<ModelPropertiesUpdated> context)
        {
            var message = context.Message;
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var jsonDataset = JsonConvert.SerializeObject(message.Dataset);
            var modelDatasetBson = BsonSerializer.Deserialize<BsonDocument>(jsonDataset);
            modelDatasetBson.Remove("BlobId");
            modelDatasetBson.Add("BlobId", message.Dataset.BlobId);

            var update = Builders<BsonDocument>.Update
                .Set("Dataset", modelDatasetBson)
                .Set("Property", message.Property.ToBsonDocument())
                .Set("Modi", context.Message.Modi)
                .Set("DisplayMethodName", context.Message.DisplayMethodName)
                .Set("Version", context.Message.Version);

            var document = await Models.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

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
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Models.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<StatusChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Status", context.Message.Status.ToString())
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Models.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<ModelStatusPersisted>(new
            {
                context.Message.Id,
                context.Message.UserId,
                context.Message.Status,
                Timestamp = DateTimeOffset.UtcNow
            });
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

            var document = await Models.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<ModelNamePersisted>(new
            {
                context.Message.Name,
                context.Message.Id,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<ModelDeleted> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id);
            if (!context.Message.Force)
                filter.Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("IsDeleted", true)
                .Set("Version", context.Message.Version);

            var document = await Models.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<ModelMoved> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("ParentId", context.Message.NewParentId)
                .Set("Version", context.Message.Version);

            var model = await Models.FindOneAndUpdateAsync(filter, update);

            if (model == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<ModelParentPersisted>(new
            {
                ParentId = context.Message.NewParentId,
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var document = await Models.FindOneAndUpdateAsync(filter, Builders<BsonDocument>.Update.Set("Version", context.Message.Version));

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            var permissions = new
            {
                context.Message.Id,
                context.Message.AccessPermissions.IsPublic,
                context.Message.AccessPermissions.Users,
                context.Message.AccessPermissions.Groups,
            }.ToBsonDocument();

            await AccessPermissions.ReplaceOneAsync(new BsonDocument("_id", context.Message.Id), permissions, new UpdateOptions { IsUpsert = true });
        }

        public async Task Consume(ConsumeContext<TargetsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Targets", context.Message.Targets)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("Version", context.Message.Version);

            var document = await Models.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<ConsensusWeightChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("ConsensusWeight", context.Message.ConsensusWeight)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("Version", context.Message.Version);

            var document = await Models.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<ModelBlobChanged> context)
        {
            var message = context.Message;
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var jsonMeta = JsonConvert.SerializeObject(message.Metadata);
            var modelMetaBson = BsonSerializer.Deserialize<BsonDocument>(jsonMeta);

            var update = Builders<BsonDocument>.Update
                .Set("Blob", new BsonDocument("_id", context.Message.BlobId).Add("Bucket", message.Bucket))
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("Metadata", modelMetaBson)
                .Set("Version", context.Message.Version);

            var document = await Models.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }
    }
}