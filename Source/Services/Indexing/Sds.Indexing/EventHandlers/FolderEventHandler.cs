using MassTransit;
using MongoDB.Driver;
using Nest;
using Sds.Indexing.Extensions;
using Sds.Osdr.Generic.Domain.Events.Folders;
using System;
using System.Threading.Tasks;

namespace Sds.Indexing.EventHandlers
{
    public class FolderEventHandler : BaseEntityEventHandler, IConsumer<FolderPersisted>, IConsumer<FolderDeleted>
    {
        const string typeName = "folder";
        const string indexName = "folders";

        public FolderEventHandler(IElasticClient elasticClient, IMongoDatabase database)
            :base(elasticClient, database, $"{typeName.ToPascalCase()}s")
        {
            
        }

        public async Task Consume(ConsumeContext<FolderDeleted> context)
        {
            await RemoveEntityAsync(indexName, typeName, context.Message.Id);
        }

        public async Task Consume(ConsumeContext<FolderPersisted> context)
        {
            await IndexEntityAsync(indexName, typeName, context.Message.Id);
        }
    }
}
