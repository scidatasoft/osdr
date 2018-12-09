using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Files;
using Sds.Osdr.Generic.Domain.Commands.Folders;
using Sds.Osdr.Generic.Domain.Commands.Models;
using Sds.Osdr.Generic.Domain.Events.Folders;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Folders
{
    public class FolderEventHandlers : IConsumer<FolderCreated>,
		                               IConsumer<StatusChanged>,
		                               IConsumer<FolderNameChanged>,
		                               IConsumer<FolderMoved>,
		                               IConsumer<FolderDeleted>,
		                               IConsumer<ProcessingProgressChanged>,
                                       IConsumer<PermissionsChanged>
    {
		private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _foldersCollection;
        private readonly IMongoCollection<BsonDocument> _nodesCollection;
        private readonly IMongoCollection<BsonDocument> _accessPermissions;

        public FolderEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _foldersCollection = _database.GetCollection<BsonDocument>("Folders");
            _nodesCollection = _database.GetCollection<BsonDocument>("Nodes");
            _accessPermissions = _database.GetCollection<BsonDocument>("AccessPermissions");
        }

		public async Task Consume(ConsumeContext<FolderCreated> context)
		{
			var folder = new
			{
				CreatedBy = context.Message.UserId,
				CreatedDateTime = context.Message.TimeStamp.UtcDateTime,
                UpdatedBy = context.Message.UserId,
                UpdatedDateTime = context.Message.TimeStamp.UtcDateTime,
                Id = context.Message.Id,
				Name = context.Message.Name,
				OwnedBy = context.Message.UserId,
                IsDeleted = false,
                ParentId = context.Message.ParentId,
				Version = context.Message.Version,
                Status = FolderStatus.Created.ToString()
			}.ToBsonDocument();

			await _foldersCollection.InsertOneAsync(folder);

            await context.Publish<FolderPersisted>(new
            {
                Id = context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                Version = context.Message.Version
            });
        }

        public async Task Consume(ConsumeContext<FolderNameChanged> context)
		{
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
				.Set("Name", context.Message.NewName)
				.Set("UpdatedBy", context.Message.UserId)
				.Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
				.Set("Version", context.Message.Version);

            var element = await _foldersCollection.FindOneAndUpdateAsync(filter, update);

            if (element == null)
                throw new ConcurrencyException(context.Message.Id);

            //await context.Publish(new FolderPersisted(context.Message.Id, context.Message.UserId));

            await context.Publish<RenamedFolderPersisted>(new
            {
                Id = context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                Version = context.Message.Version
            });
        }

        public async Task Consume(ConsumeContext<FolderMoved> context)
		{
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
				.Set("ParentId", context.Message.NewParentId)
				.Set("UpdatedBy", context.Message.UserId)
				.Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
				.Set("Version", context.Message.Version);

            var element = await _foldersCollection.FindOneAndUpdateAsync(filter, update);

            if (element == null)
                throw new ConcurrencyException(context.Message.Id);

            //await context.Publish(new FolderPersisted(context.Message.Id, context.Message.UserId));

            await context.Publish<MovedFolderPersisted>(new
            {
                Id = context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                Version = context.Message.Version
            });
        }

        public async Task Consume(ConsumeContext<FolderDeleted> context)
		{
            var filter = new BsonDocument("_id", context.Message.Id);
            if(!context.Message.Force)
                filter.Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
				.Set("IsDeleted", true)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var element = await _foldersCollection.FindOneAndUpdateAsync(filter, update);

            if (element == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<DeletedFolderPersisted>(new
            {
                Id = context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                Version = context.Message.Version
            });

            var childNodes = await _nodesCollection.Find(new BsonDocument("ParentId", context.Message.Id).Add("IsDeleted", new BsonDocument("$ne",true)))
                .Project("{Type:1, Version:1}")
                .ToListAsync();

            foreach (var node in childNodes)
            {
                switch (node["Type"].AsString.ToLower())
                {
                    case "folder":
                        await context.Publish<DeleteFolder>(new
                        {
                            Id = node["_id"].AsGuid,
                            UserId = context.Message.UserId,
                            CorrelationId = context.Message.CorrelationId,
                            ExpectedVersion = node["Version"].AsInt32,
                            Force = context.Message.Force
                        });

                        break;

                    case "file":
                        await context.Publish<DeleteFile>(new
                        {
                            Id = node["_id"].AsGuid,
                            UserId = context.Message.UserId,
                            ExpectedVersion = node["Version"].AsInt32,
                            Force = context.Message.Force
                        });
                        break;

                    case "model":
                        await context.Publish<DeleteModel>(new
                        {
                            Id = node["_id"].AsGuid,
                            UserId = context.Message.UserId,
                            ExpectedVersion = node["Version"].AsInt32,
                            Force = context.Message.Force
                        });
                        break;
                }
            }
		}

		public async Task Consume(ConsumeContext<StatusChanged> context)
		{
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
				.Set("Status", context.Message.Status.ToString())
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Version", context.Message.Version);

            var element = await _foldersCollection.FindOneAndUpdateAsync(filter, update);

            if (element == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<StatusPersisted>(new
            {
                context.Message.Id,
                ParentId = element["ParentId"].AsGuid,
                Status = context.Message.Status,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version
            });
        }

        public async Task Consume(ConsumeContext<ProcessingProgressChanged> context)
		{
			var filter = new BsonDocument("_id", context.Message.Id);
			var update = Builders<BsonDocument>.Update
				.Push("Progress", new
				{
					CorrelationId = context.Message.CorrelationId,
					Message = context.Message.ProgressMessage,
					UpdatedDateTime = context.Message.TimeStamp.UtcDateTime
                });
            // We do not increment version in the Folder aggregate!
//				.Inc("Version", 1);

			await _foldersCollection.UpdateOneAsync(filter, update);
            //await context.Publish(new FolderPersisted(context.Message.Id, context.Message.UserId));
        }

        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var document = await _foldersCollection.FindOneAndUpdateAsync(filter, Builders<BsonDocument>.Update.Set("Version", context.Message.Version));

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
