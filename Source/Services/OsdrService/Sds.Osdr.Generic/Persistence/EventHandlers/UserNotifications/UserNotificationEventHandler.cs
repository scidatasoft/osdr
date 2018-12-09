using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Events.UserNotifications;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.UserNotifications
{
    public class UserNotificationEventHandler : IConsumer<UserNotificationCreated>, 
                                                IConsumer<UserNotificationDeleted>
    {
        IMongoDatabase _database;
        IMongoCollection<BsonDocument> _userNotficationCollection;

        public UserNotificationEventHandler(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _userNotficationCollection = _database.GetCollection<BsonDocument>("UserNotifications");
        }

        public async Task Consume(ConsumeContext<UserNotificationCreated> context)
        {
            var bsonDocument = new
            {
                context.Message.Id,
                context.Message.Version,
                TimeStamp = context.Message.TimeStamp.UtcDateTime,
                context.Message.NodeType,
                context.Message.NodeId,
                context.Message.ParentNodeId,
                OwnedBy = context.Message.UserId,
                context.Message.EventTypeName,
                EventPayload = BsonDocument.Parse(context.Message.EventPayload.ToString())
            }.ToBsonDocument();

            await _userNotficationCollection.InsertOneAsync(bsonDocument);
            await context.Publish<UserNotificationPersisted>(
                new
                {
                    context.Message.Id,
                    Notification = new
                    {
                        context.Message.UserId,
                        context.Message.NodeId,
                        context.Message.NodeType,
                        context.Message.ParentNodeId,
                        context.Message.EventPayload,
                        context.Message.EventTypeName
                    }
                });
        }

        public async Task Consume(ConsumeContext<UserNotificationDeleted> context)
        {
            await _userNotficationCollection.UpdateOneAsync(new BsonDocument("_id", context.Message.Id),
                Builders<BsonDocument>.Update
                .Set("IsDeleted", true)
                .Set("Version", context.Message.Version));
        }
    }
}
