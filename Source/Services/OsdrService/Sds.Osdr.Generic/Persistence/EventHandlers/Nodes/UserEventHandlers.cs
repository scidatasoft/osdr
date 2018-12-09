using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Events.Nodes;
using Sds.Osdr.Generic.Domain.Events.Users;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Nodes
{
    public class UserEventHandlers : BaseNodeEventHandlers,
                                     IConsumer<UserCreated>,
                                     IConsumer<UserUpdated>
    {
        public UserEventHandlers(IMongoDatabase database)
            : base(database)
        {
        }

        public async Task Consume(ConsumeContext<UserCreated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id);
            var options = new UpdateOptions { IsUpsert = true };
            var update = Builders<BsonDocument>.Update
                .Set("_id", context.Message.Id)
                .Set("Type", NodeType.User.ToString())
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("DisplayName", context.Message.DisplayName)
                .Set("FirstName", context.Message.FirstName)
                .Set("LastName", context.Message.LastName)
                .Set("LoginName", context.Message.LoginName)
                .Set("Email", context.Message.Email)
                .Set("Avatar", context.Message.Avatar)
                .Set("Version", context.Message.Version);

            await Nodes.UpdateOneAsync(filter, update, options);

            await context.Publish(new NodePersisted<UserCreated>(context.Message, context.Message.Id, context.Message.Id, NodeType.User.ToString()));

            await context.Publish<Domain.Events.Nodes.UserPersisted>(new
            {
                Id = context.Message.Id,
                TimeStamp = DateTimeOffset.UtcNow,
                Version = context.Message.Version
            });
        }

        public async Task Consume(ConsumeContext<UserUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id).Add("Version", context.Message.Version - 1);
            var update = Builders<BsonDocument>.Update
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("DisplayName", context.Message.DisplayName)
                .Set("FirstName", context.Message.FirstName)
                .Set("LastName", context.Message.LastName)
                .Set("Version", context.Message.Version);

            var node = await Nodes.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            await context.Publish(new NodePersisted<UserUpdated>(context.Message, context.Message.Id, context.Message.Id, NodeType.User.ToString()));
        }
    }
}
