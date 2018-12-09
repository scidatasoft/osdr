using CQRSlite.Domain.Exception;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Events.Users;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Users
{
    public class UserEventHandlers : IConsumer<UserCreated>,
                                     IConsumer<UserUpdated>
    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Users { get { return _database.GetCollection<BsonDocument>("Users"); } }

        public UserEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<UserCreated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id);
            var options = new UpdateOptions { IsUpsert = true };
            var update = Builders<BsonDocument>.Update
                .Set("_id", context.Message.Id)
                .Set("CreatedBy", context.Message.UserId)
                .Set("CreatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set(nameof(context.Message.DisplayName), context.Message.DisplayName)
                .Set(nameof(context.Message.FirstName), context.Message.FirstName)
                .Set(nameof(context.Message.LastName), context.Message.LastName)
                .Set(nameof(context.Message.LoginName), context.Message.LoginName)
                .Set(nameof(context.Message.Email), context.Message.Email)
                .Set(nameof(context.Message.Avatar), context.Message.Avatar)
                .Inc("Version", 1);

            await Users.UpdateOneAsync(filter, update, options);

            await context.Publish<UserPersisted>(new
            {
                Id = context.Message.Id,
                DisplyaName = context.Message.DisplayName,
                FirstName = context.Message.FirstName,
                LastName = context.Message.LastName,
                TimeStamp = DateTimeOffset.UtcNow,
                Version = context.Message.Version
            });
        }

        public async Task Consume(ConsumeContext<UserUpdated> context)
        {
            var filter = new BsonDocument("_id", context.Message.Id)/*.Add("Version", context.Message.Version - 1)*/;
            var update = Builders<BsonDocument>.Update
                .Set("_id", context.Message.Id)
                .Set("UpdatedBy", context.Message.UserId)
                .Set("UpdatedDateTime", context.Message.TimeStamp.UtcDateTime)
                .Set(nameof(context.Message.DisplayName), context.Message.DisplayName)
                .Set(nameof(context.Message.FirstName), context.Message.FirstName)
                .Set(nameof(context.Message.LastName), context.Message.LastName)
                .Set(nameof(context.Message.Email), context.Message.Email)
                .Set(nameof(context.Message.Avatar), context.Message.Avatar)
                .Inc("Version", 1);

            var node = await Users.FindOneAndUpdateAsync(filter, update);

            if (node == null)
                throw new ConcurrencyException(context.Message.Id);

            //await context.Publish(new UserPersisted(context.Message.Id, context.Message.DisplayName, context.Message.FirstName, context.Message.LastName));
        }
    }
}
