using MassTransit;
using MongoDB.Driver;
using Nest;
using Sds.Indexing.Extensions;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System.Threading.Tasks;

namespace Sds.Indexing.EventHandlers
{
    public class RecordEventHandler : BaseEntityEventHandler, IConsumer<RecordPersisted>, IConsumer<StatusPersisted>, IConsumer<RecordDeleted>, IConsumer<PermissionsChanged>
    {
        const string typeName = "record";
        const string indexName = "records";

        public RecordEventHandler(IElasticClient elasticClient, IMongoDatabase database)
            : base(elasticClient, database, $"{typeName.ToPascalCase()}s")
        {

        }

        public async Task Consume(ConsumeContext<RecordPersisted> context)
        {
            await IndexEntityAsync(indexName, typeName, context.Message.Id);
        }

        public async Task Consume(ConsumeContext<StatusPersisted> context)
        {
            if (context.Message.Status == Osdr.RecordsFile.Domain.RecordStatus.Processed)
            await IndexEntityAsync(indexName, typeName, context.Message.Id);
        }

        public async Task Consume(ConsumeContext<RecordDeleted> context)
        {
            await RemoveEntityAsync(indexName, typeName, context.Message.Id);
        }

        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            await SetPermissions(indexName, typeName, context.Message.Id.ToString(), context.Message.AccessPermissions);
        }
    }
}
