using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Domain.Modules;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Events.Files;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Persistence.EventHandlers.Users
{
    public class FilesCountersEventHandlers : IConsumer<FileCreated>,
                                              IConsumer<FileDeleted>
    {
        protected readonly IMongoDatabase _database;

        protected IMongoCollection<BsonDocument> Users { get { return _database.GetCollection<BsonDocument>("Users"); } }

        public FilesCountersEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<FileCreated> context)
        {
            var userView = Users.Find(new BsonDocument("_id", context.Message.UserId)).FirstOrDefault().AsBsonDocument;
            string counterName = GetCounterName(context.Message.FileType); 

            if(!counterName.Equals(""))
            {
                //    long? counter;
                //    try
                //    {
                //        counter = userView["Counters"][counterName].AsNullableInt64;
                //    }
                //    catch
                //    {
                //        counter = null;
                //    }

                    var filter = new BsonDocument("_id", (Guid)userView["_id"]);
                    UpdateDefinition<BsonDocument> update;
                //    if (counter == null || counter <= 0)
                //    {
                //        update = Builders<BsonDocument>.Update
                //            .Set($"Counters.{counterName}", (long)1);
                //    }
                //    else

                //    //
                //    {
                update = Builders<BsonDocument>.Update
                        .Inc($"Counters.{counterName}", 1);
                //}
                await Users.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<BsonDocument>() { IsUpsert = true });
                //check
            }
        }

        public async Task Consume(ConsumeContext<FileDeleted> context)
        {
            var userId = context.Message.UserId;
            var userView = Users.Find(new BsonDocument("_id", userId)).FirstOrDefault().AsBsonDocument;
            string counterName = GetCounterName(context.Message.FileType);
            if (!counterName.Equals(""))
            {
                BsonValue counter; 
                try
                {
                    counter = userView["Counters"][counterName];
                }
                catch
                {
                    counter = null;
                }

                var filter = new BsonDocument("_id", userId);
                UpdateDefinition<BsonDocument> update;
                if (counter == null || counter <= 0)
                {
                    update = Builders<BsonDocument>.Update
                        .Set($"Counters.{counterName}", 0);
                }
                else
                {
                    update = Builders<BsonDocument>.Update
                        .Inc($"Counters.{counterName}", -1);
                }

                await Users.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<BsonDocument>() { IsUpsert = true  });
            }
        }

        private string GetCounterName(FileType type)
        {
            switch (type)
            {
                case FileType.Pdf:
                case FileType.Office:
                    return "Documents";
                case FileType.Image:
                case FileType.Model:
                case FileType.Tabular:
                case FileType.WebPage:
                    return type.ToString() + "s";
                default:
                    return "";
            }
        }
    }
}
