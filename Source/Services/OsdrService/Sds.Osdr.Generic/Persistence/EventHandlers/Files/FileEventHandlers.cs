using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Events.Files;
using Sds.Osdr.Generic.Extensions;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Files
{
    public class FileEventHandlers : IConsumer<FileCreated>,
                                     IConsumer<StatusChanged>,
                                     IConsumer<ImageAdded>,
                                     IConsumer<ProcessingProgressChanged>,
                                     IConsumer<FileNameChanged>,
                                     IConsumer<FileMoved>,
                                     IConsumer<FileDeleted>,
                                     IConsumer<PermissionsChanged>
    {
        private readonly IMongoDatabase database;
        private IMongoCollection<BsonDocument> Files { get { return database.GetCollection<BsonDocument>("Files"); } }
        private IMongoCollection<BsonDocument> _nodes;
        private IMongoCollection<BsonDocument> _accessPermissions => database.GetCollection<BsonDocument>("AccessPermissions");

        public FileEventHandlers(IMongoDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            _nodes = database.GetCollection<BsonDocument>("Nodes");
        }

        public async Task Consume(ConsumeContext<FileCreated> context)
        {
            var document = new BsonDocument("_id", context.Message.Id)
                .Set("Blob", (new { context.Message.Bucket, Id = context.Message.BlobId, Length = context.Message.Length, Md5 = context.Message.Md5 }).ToBsonDocument())
                .Set("SubType", context.Message.FileType.ToString())
                .Set("OwnedBy", context.Message.UserId)
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("ParentId", context.Message.ParentId)
                .Set("Name", context.Message.FileName)
                .Set("Status", context.Message.FileStatus.ToString())
                .Set("Version", context.Message.Version);

            await Files.InsertOneAsync(document);

            await context.Publish(new FilePersisted(context.Message.Id, context.Message.UserId, context.Message.FileName, context.Message.Bucket));
        }

        public async Task Consume(ConsumeContext<StatusChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Status", context.Message.Status.ToString())
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            Log.Debug($"GenericFile Persistance: StatusPersisted({context.Message.Status}) {context.Message.Id}");

            await context.Publish(new StatusPersisted(context.Message.Id, context.Message.UserId, context.Message.Status));
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

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<ProcessingProgressChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Push("Progress", new
                {
                    Total = context.Message.Total,
                    Processed = context.Message.Processed,
                    Failed = context.Message.Failed,
                    UpdatedDateTime = context.Message.TimeStamp.UtcDateTime
                })
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new ProcessingProgressPersisted(context.Message.Id, context.Message.UserId, context.Message.Total, processed: context.Message.Processed, failed: context.Message.Failed));
        }

        public async Task Consume(ConsumeContext<FileNameChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
                .Set("Name", context.Message.NewName)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new FileNamePersisted(context.Message.Id, context.Message.UserId, context.Message.NewName));
        }

        public async Task Consume(ConsumeContext<FileMoved> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
                .Set("ParentId", context.Message.NewParentId)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new FileParentPersisted(context.Message.Id, context.Message.UserId, context.Message.NewParentId));
        }

        public async Task Consume(ConsumeContext<FileDeleted> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id);
            if (!context.Message.Force)
                filter.Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("IsDeleted", true)
                .Set("Version", context.Message.Version);

            var document = await Files.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<FileDeletionPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var document = await Files.FindOneAndUpdateAsync(filter, Builders<BsonDocument>.Update.Set("Version", context.Message.Version));

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            var permissions = new
            {
                context.Message.Id,
                context.Message.AccessPermissions.IsPublic,
                context.Message.AccessPermissions.Users,
                context.Message.AccessPermissions.Groups,
            }.ToBsonDocument();

            await _accessPermissions.ReplaceOneAsync(new BsonDocument("_id", context.Message.Id), permissions, new UpdateOptions { IsUpsert = true });

            await context.Publish<PermissionChangedPersisted>(new
            {
                context.Message.Id,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }
    }
}
