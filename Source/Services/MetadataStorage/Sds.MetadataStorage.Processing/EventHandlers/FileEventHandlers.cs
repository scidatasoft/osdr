using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.MetadataStorage.Domain.Commands;
using Sds.MetadataStorage.Domain.Events;
using Sds.Osdr.RecordsFile.Domain.Events.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.MetadataStorage.Processing.EventHandlers
{
    public class FileEventHandlers : IConsumer<GenerateMetadata>
    {
        IMongoDatabase _database;
        IMongoCollection<BsonDocument> _metadataCollection;
        IMongoCollection<BsonDocument> _records;
        IMongoCollection<BsonDocument> _files;

        public FileEventHandlers(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _metadataCollection = _database.GetCollection<BsonDocument>("Metadata");
            _records = _database.GetCollection<BsonDocument>("Records");
            _files = _database.GetCollection<BsonDocument>("Files");
        }

        public async Task Consume(ConsumeContext<GenerateMetadata> context)
        {
            var filter = new BsonDocument("FileId", context.Message.FileId).Add("Status", new BsonDocument("$ne", "Failed"));
            var fields = await _files.Aggregate().Match(new BsonDocument("_id", context.Message.FileId))
                .Project("{\"Properties.Fields\":1}")
                .FirstOrDefaultAsync();

            IEnumerable<string> fieldNames = new string[] { };

            if (fields.GetValue("Properties", null)?.AsBsonDocument.Contains("Fields") ?? false)
                fieldNames = fields["Properties"]["Fields"].AsBsonArray.Select(b => b?.AsBsonValue.AsString);

            var infoboxMetadata = new
            {
                Id = Guid.NewGuid(),
                context.Message.FileId,
                Title = "Fields",
                FileType = "MOL", // | application/osdr-chemical-file+mol",
                InfoBoxType = "fields",
                Screens = new[] { new { Title = "", ScreenType = "composite", ScreenParts = new List<object>() } }
            };

            var entityMetadatas = new
            {
                Id = context.Message.FileId,
                Fields = new List<object>()
            };

            foreach (var fieldName in fieldNames)
            {
                var typeQualifer = new TypeQualifier();

                using (var cursor = await _records.Aggregate(new AggregateOptions { BatchSize = 1000 }).Match(filter)
                    .ReplaceRoot<BsonDocument>("$Properties")
                    .Unwind("Fields")
                    .Match(new BsonDocument("Fields.Name", fieldName))
                    .Project("{\"Fields.Value\":1}").ToCursorAsync())
                {
                    while (await cursor.MoveNextAsync())
                        foreach (var document in cursor.Current)
                            typeQualifer.Qualify(document["Fields"]["Value"].AsString);
                }

                var screenParts = new
                {
                    Title = fieldName,
                    ResponseType = "text-response",
                    typeQualifer.DataType,
                    ResponseTarget = $"Properties.Fields[@Name='{fieldName}'].Value"
                };

                infoboxMetadata.Screens[0].ScreenParts.Add(screenParts);

                if (typeQualifer.MaxValue != null)
                    entityMetadatas.Fields.Add(new
                    {
                        Name = fieldName,
                        typeQualifer.DataType,
                        typeQualifer.MinValue,
                        typeQualifer.MaxValue
                    });
                else
                    entityMetadatas.Fields.Add(new
                    {
                        Name = fieldName,
                        typeQualifer.DataType,
                    });
            }

            await _metadataCollection.FindOneAndReplaceAsync(new BsonDocument("FileId", context.Message.FileId).Add("InfoBoxType", "fields"), infoboxMetadata.ToBsonDocument(), new FindOneAndReplaceOptions<BsonDocument> { IsUpsert = true });
            await _metadataCollection.FindOneAndReplaceAsync(new BsonDocument("_id", context.Message.FileId), entityMetadatas.ToBsonDocument(), new FindOneAndReplaceOptions<BsonDocument> { IsUpsert = true });

            await context.Publish<MetadataGenerated>(new { Id = context.Message.FileId });
        }
    }
}