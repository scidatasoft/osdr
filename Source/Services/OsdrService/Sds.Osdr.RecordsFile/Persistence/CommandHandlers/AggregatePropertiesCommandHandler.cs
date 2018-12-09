using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.RecordsFile.Domain.Commands.Files;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.Persistence.CommandHandlers
{
    public class AggregatePropertiesCommandHandler : IConsumer<AggregateProperties>
    {
        private readonly IMongoDatabase database;
        private IMongoCollection<BsonDocument> Records { get { return database.GetCollection<BsonDocument>("Records"); } }


        public AggregatePropertiesCommandHandler(IMongoDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task Consume(ConsumeContext<AggregateProperties> context)
        {
            var fileId = context.Message.Id;

            var properties = Records.Aggregate()
               .Match(new BsonDocument { { "FileId", fileId } })
               .Unwind(d => d["Properties.ChemicalProperties"])
               .Group(new BsonDocument { { "_id", "$Properties.ChemicalProperties.Name" } })
               .ToList()
               .Select(d=> d.GetValue(0).ToString())
               ;

            await context.Publish<AddAggregatedProperties>(new
            {
                Id = fileId,
                Properties = properties,
                UserId = context.Message.UserId
            });
        }
    }
}
