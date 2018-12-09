using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.Persistence.EventHandlers.Records
{
    public class RecordsEventHandlers : IConsumer<RecordCreated>,
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
        private readonly IMongoDatabase database;

        private IMongoCollection<BsonDocument> Records { get { return database.GetCollection<BsonDocument>("Records"); } }
        private IMongoCollection<BsonDocument> AccessPermissions => database.GetCollection<BsonDocument>("AccessPermissions");

        public RecordsEventHandlers(IMongoDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<RecordCreated> context)
        {
            var document = new BsonDocument("_id", context.Message.Id)
                .Set("Type", context.Message.RecordType.ToString())
                .Set("Name", context.Message.Index.ToString())
                .Set("FileId", context.Message.FileId)
                .Set("Blob", (new { context.Message.Bucket, Id = context.Message.BlobId }).ToBsonDocument())
                .Set("OwnedBy", context.Message.UserId)
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Index", context.Message.Index)
                .Set("Properties", (new { Fields = context.Message.Fields }).ToBsonDocument())
                .Set("Status", RecordStatus.Created.ToString())
                .Set("Version", context.Message.Version);

            await Records.InsertOneAsync(document);

            await context.Publish<RecordPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<StatusChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Status", context.Message.Status.ToString())
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Records.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<StatusPersisted>(new
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
                .Push("Images", context.Message.Image)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Records.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<RecordPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<PropertiesAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Properties.ChemicalProperties", context.Message.Properties)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Records.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<RecordPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<IssueAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Push("Properties.Issues", new
                {
                    Code = context.Message.Issue.Code,
                    Message = context.Message.Issue.Message,
                    Severity = context.Message.Issue.Severity.ToString(),
                    AuxInfo = context.Message.Issue.AuxInfo,
                    Title = context.Message.Issue.Title
                })
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Records.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<RecordPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<IssuesAdded> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            foreach (var issue in context.Message.Issues)
            {
                update = update.Push("Properties.Issues", new
                {
                    Code = issue.Code,
                    Message = issue.Message,
                    Severity = issue.Severity.ToString(),
                    AuxInfo = issue.AuxInfo,
                    Title = issue.Title
                });
            }

            var document = await Records.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<RecordPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
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

            var document = await Records.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<DeletionPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<FieldsUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("Properties.Fields", context.Message.Fields)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var document = await Records.FindOneAndUpdateAsync(filter, update);

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<RecordPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<InvalidRecordCreated> context)
        {
            var document = new BsonDocument("_id", context.Message.Id)
                .Set("Type", context.Message.RecordType.ToString())
                .Set("FileId", context.Message.FileId)
                .Set("OwnedBy", context.Message.UserId)
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Index", context.Message.Index)
                .Set("Status", RecordStatus.Failed.ToString())
                .Set("Message", context.Message.Error)
                .Set("Version", context.Message.Version);

            await Records.InsertOneAsync(document);

            await context.Publish<RecordPersisted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var document = await Records.FindOneAndUpdateAsync(filter, Builders<BsonDocument>.Update.Set("Version", context.Message.Version));

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
            await context.Publish<PermissionChangedPersisted>(new
            {
                context.Message.Id,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });
        }
    }
}
