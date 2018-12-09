using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Events.Files;
using Sds.Osdr.Generic.Domain.Events.Nodes;
using Sds.Osdr.Generic.Extensions;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Nodes
{
    public class FileEventHandlers : BaseNodeEventHandlers,
                                     IConsumer<FileCreated>,
                                     IConsumer<FileMoved>,
                                     IConsumer<FileNameChanged>,
                                     IConsumer<FileDeleted>,
                                     IConsumer<StatusChanged>,
                                     IConsumer<ImageAdded>,
                                     IConsumer<PermissionsChanged>
    {
        public FileEventHandlers(IMongoDatabase database)
            : base(database)
        {
        }

        public async Task Consume(ConsumeContext<FileCreated> context)
        {
            var document = new BsonDocument("_id", context.Message.Id)
                .Set("Type", NodeType.File.ToString())
                .Set("SubType", context.Message.FileType.ToString())
                .Set("Blob", (new { context.Message.Bucket, Id = context.Message.BlobId, Length = context.Message.Length, Md5 = context.Message.Md5 }).ToBsonDocument())
                .Set("Status", context.Message.FileStatus.ToString())
                .Set("OwnedBy", context.Message.UserId)
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("Name", context.Message.FileName)
                .Set("ParentId", context.Message.ParentId ?? context.Message.UserId)
                .Set("Version", context.Message.Version);

            await Nodes.InsertOneAsync(document);

            await context.Publish<Domain.Events.Nodes.FilePersisted>(new
            {
                context.Message.Id,
                context.Message.Version,
                context.Message.UserId,
                context.Message.ParentId,
                context.Message.FileName,
                TimeStamp = DateTime.UtcNow
            });
            
            //await context.Publish(new NodePersisted<FileCreated>(context.Message, context.Message.Id, context.Message.UserId, NodeType.File.ToString(), context.Message.ParentId));
        }

        public async Task Consume(ConsumeContext<FileMoved> context)
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

            filter = new BsonDocument("_id", context.Message.NewParentId);
            node = await Nodes.Find(filter).FirstAsync();
            var folderName = node["Name"].AsString;

            await context.Publish<MovedFilePersisted>(new
            {
                context.Message.Id,
                context.Message.Version,
                context.Message.UserId,
                context.Message.NewParentId,
                context.Message.OldParentId,
                TargetFolderName = folderName,
                TimeStamp = DateTime.UtcNow
            });

            //await context.Publish(new NodePersisted<FileMoved>(context.Message, context.Message.Id, context.Message.UserId, NodeType.File.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }

        public async Task Consume(ConsumeContext<FileNameChanged> context)
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

            await context.Publish<RenamedFilePersisted>(new
            {
                context.Message.Id,
                context.Message.Version,
                context.Message.UserId,
                context.Message.NewName,
                TimeStamp = DateTime.UtcNow,
                ParentId = node["ParentId"].AsGuid
            });
            //await context.Publish(new NodePersisted<FileNameChanged>(context.Message, context.Message.Id, context.Message.UserId, NodeType.File.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }

        public async Task Consume(ConsumeContext<FileDeleted> context)
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

            await context.Publish<DeletedFilePersisted>(new
            {
                context.Message.Id,
                context.Message.Version,
                context.Message.UserId,
                TimeStamp = DateTime.UtcNow,
                ParentId = node["ParentId"].AsGuid
            });

            //await context.Publish(new NodePersisted<FileDeleted>(context.Message, context.Message.Id, context.Message.UserId, NodeType.File.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
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

            Log.Debug($"GenericFile Persistance: NodeStatusPersisted({context.Message.Status}) {context.Message.Id}");

            //await context.Publish(
            //    new NodePersisted<StatusChanged>(context.Message, context.Message.Id, context.Message.UserId, NodeType.File.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));

            await context.Publish<NodeStatusPersisted>(new
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

            //await context.Publish(new NodePersisted<ImageAdded>(context.Message, context.Message.Id, context.Message.UserId, NodeType.File.ToString(), node?.GetValue("ParentId", null)?.AsNullableGuid));
        }

        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

            var document = await Nodes.FindOneAndUpdateAsync(filter, Builders<BsonDocument>.Update.Set("Version", context.Message.Version));

            if (document == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish<Domain.Events.Nodes.PermissionChangedPersisted>(new
            {
                context.Message.Id,
                context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                ParentId = document["ParentId"].AsGuid,
                context.Message.AccessPermissions,
            });
        }
   //     public async Task Consume(ConsumeContext<Sds.Osdr.Generic.Domain.Events.Nodes.PermissionChangedPersisted> context)
   //     {
   //         var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);

   //         var document = await Nodes.FindOneAndUpdateAsync(filter, Builders<BsonDocument>.Update.Set("Version", context.Message.Version));

   //         if (document == null)
   //             throw new ConcurrencyException(context.Message.Id);

   //         await context.Publish<Domain.Events.Nodes.PermissionChangedPersisted>(new
   //         {
   //             context.Message.Id,
   //             context.Message.UserId,
   //             TimeStamp = DateTimeOffset.UtcNow,
   //             ParentId = document["ParentId"].AsString
   //         });
   //     }
    }
}
