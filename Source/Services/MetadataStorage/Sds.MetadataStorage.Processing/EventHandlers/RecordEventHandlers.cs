using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.MetadataStorage.Processing.EventHandlers
{
    public class RecordEventHandlers : IConsumer<PropertiesAdded>
    {
        IMongoDatabase _database;
        IMongoCollection<BsonDocument> _metadataCollection;

        public RecordEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _metadataCollection = _database.GetCollection<BsonDocument>("Metadata");
        }

        public async Task Consume(ConsumeContext<PropertiesAdded> context)
        {
            var existingMetaDataId = await _metadataCollection.Find(new BsonDocument("InfoBoxType", "chemical-properties"))
                .Project("{_id:1}")
                .SingleOrDefaultAsync();

            if (existingMetaDataId is null)
            {
                var screenParts = context.Message.Properties.Select(p => new
                {
                    Title = p.Name,
                    ResponseType = "text-response",
                    DataType = "string",
                    ResponseTarget = $"Properties.ChemicalProperties[@Name='{p.Name}'].Value"
                });

                var metadata = new
                {
                    Id = Guid.NewGuid(),
                    Title = "Chemical Properties",
                    FileType = "MOL", // | application/osdr-chemical-file+mol",
                    InfoBoxType = "chemical-properties",
                    ReadOnly = true,
                    Screens = new[] { new { Title = "", ScreenType = "composite", ScreenParts = screenParts } }
                }.ToBsonDocument();

                await _metadataCollection.FindOneAndReplaceAsync(new BsonDocument("InfoBoxType", "chemical-properties"), metadata, new FindOneAndReplaceOptions<BsonDocument> { IsUpsert = true });
            }
        }
    }
}
