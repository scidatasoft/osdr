using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Events.Folders;
using Sds.Osdr.Generic.Domain.Events.Nodes;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Nodes
{
    public class FolderContentEventHandlers : BaseNodeEventHandlers,
        IConsumer<FolderCreated>,
		IConsumer<StatusChanged>,
		IConsumer<FolderMoved>,
		IConsumer<FolderNameChanged>,
		IConsumer<FolderDeleted>,
		IConsumer<ProcessingProgressChanged>,
        IConsumer<PermissionsChanged>
    {
		public FolderContentEventHandlers(IMongoDatabase database)
			: base(database)
		{
		}

		public async Task Consume(ConsumeContext<FolderCreated> context)
		{
            var document = new BsonDocument("_id", context.Message.Id)
                .Set("Type", NodeType.Folder.ToString())
                .Set("OwnedBy", context.Message.UserId)
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Name", context.Message.Name)
				.Set("ParentId", context.Message.ParentId)
				.Set("Version", context.Message.Version);

            await Nodes.InsertOneAsync(document);

            await context.Publish<Domain.Events.Nodes.FolderPersisted>(new
            {
                context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version,
                context.Message.ParentId,
                context.Message.Name,
                context.Message.UserId
            });
        }

        public async Task Consume(ConsumeContext<FolderMoved> context)
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

            await context.Publish<Domain.Events.Nodes.MovedFolderPersisted>(new
            {
                context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version,
                context.Message.OldParentId,
                context.Message.NewParentId,
                context.Message.UserId
            });
        }

        public async Task Consume(ConsumeContext<FolderNameChanged> context)
		{
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
				.Set("UpdatedBy", context.Message.UserId)
				.Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
				.Set("Name", context.Message.NewName)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<Domain.Events.Nodes.RenamedFolderPersisted>(new
            {
                context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version,
                context.Message.NewName,
                context.Message.UserId,
                ParentId = node["ParentId"].AsGuid
            });
        }

        public async Task Consume(ConsumeContext<FolderDeleted> context)
		{
            var filter = new BsonDocument("_id", context.Message.Id);
            if (!context.Message.Force)
                filter.Add("Version", context.Message.Version - 1);

            var update = Builders<BsonDocument>.Update
				.Set("UpdatedBy", context.Message.UserId)
				.Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
				.Set("IsDeleted", true)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<Domain.Events.Nodes.DeletedFolderPersisted>(new
            {
                context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version,
                context.Message.UserId,
                ParentId = node["ParentId"].AsGuid
            });
        }

        public async Task Consume(ConsumeContext<ProcessingProgressChanged> context)
		{
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Push("Progress", new
                {
                    context.Message.CorrelationId,
                    Message = context.Message.ProgressMessage,
                    UpdatedDateTime = context.Message.TimeStamp
                });

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new NodePersisted<ProcessingProgressChanged>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Folder.ToString(), node?.GetValue("ParentId").AsNullableGuid));
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

            await context.Publish<NodeStatusPersisted>(new
            {
                context.Message.Id,
                ParentId = node["ParentId"].AsGuid,
                Status = context.Message.Status,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                context.Message.Version
            });
        }
        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var document = await Nodes.FindOneAndUpdateAsync(filter, Builders<BsonDocument>.Update.Set("Version", context.Message.Version));

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

//            await context.Publish(new NodePersisted<PermissionsChanged>(context.Message, context.Message.Id, context.Message.UserId, NodeType.Folder.ToString(), document["ParentId"].AsNullableGuid));
            
            await context.Publish<Domain.Events.Nodes.PermissionChangedPersisted>(new
            {
                context.Message.Id,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                ParentId = document["ParentId"].AsGuid,
                context.Message.AccessPermissions
            });
        }
    }
}