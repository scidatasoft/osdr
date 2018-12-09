using CQRSlite.Domain.Exception;
using Elasticsearch.Net;
using MassTransit;
using MongoDB.Driver;
using Nest;
using Sds.Indexing.Extensions;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Storage.Blob.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sds.Indexing.EventHandlers
{
    public class ModelEventHandler : BaseEntityEventHandler, IConsumer<ModelPersisted>, IConsumer<ModelStatusPersisted>, IConsumer<ModelDeleted>,
                                    IConsumer<ModelNamePersisted>, IConsumer<ModelParentPersisted>, IConsumer<PermissionsChanged>
    {
        const string typeName = "model";
        const string indexName = "models";
        IBlobStorage _blobStorage;

        public ModelEventHandler(IElasticClient elasticClient, IMongoDatabase database, IBlobStorage blobStorage)
            : base(elasticClient, database, $"{typeName.ToPascalCase()}s")
        {
            _blobStorage = blobStorage;
        }

        public async Task Consume(ConsumeContext<ModelDeleted> context)
        {
            await RemoveEntityAsync(indexName, typeName, context.Message.Id);
        }

        public async Task Consume(ConsumeContext<ModelParentPersisted> context)
        {
            await UpdateModelDocument(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<ModelNamePersisted> context)
        {
            await UpdateModelDocument(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<ModelPersisted> context)
        {
            await IndexEntityAsync(indexName, typeName, context.Message.Id);
        }

        public async Task Consume(ConsumeContext<ModelStatusPersisted> context)
        {
            if (context.Message.Status == ModelStatus.Processed)
                await UpdateModelDocument(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<PermissionsChanged> context)
        {
            await SetPermissions(indexName, typeName, context.Message.Id.ToString(), context.Message.AccessPermissions);
        }

        private async Task CreateFileDocument(Guid id)
        {
            dynamic entity = await GetEntityFromDatabase(id);

            using (var ms = new MemoryStream())
            {
                await _blobStorage.DownloadFileToStreamAsync(entity.Blob.id, ms, entity.Blob.Bucket);
                entity.Blob.Base64Content = Convert.ToBase64String(ms.ToArray());
                try
                {
                    await _elasticClient.IndexAsync(new IndexRequest<object>(entity, indexName, typeName, id) { Pipeline = "process_blob" });
                }
                catch (ElasticsearchClientException e)
                {
                    Log.Error($"Create document response: {e.Response.ServerError}, file: {entity.FileName}, id = '{id}'");
                }
            }
        }

        private async Task UpdateModelDocument(Guid id)
        {
            dynamic entity = await GetEntityFromDatabase(id);
            bool entityIsDeleted = ((IDictionary<string, object>)entity).ContainsKey("IsDeleted") ? entity.IsDeleted : false;

            if (!entityIsDeleted)
            {
                try
                {
                    await _elasticClient.IndexAsync<object>(new IndexRequest<object>(entity, indexName, typeName, id));
                }
                catch (ElasticsearchClientException e)
                {
                    Log.Error($"Update document response: {e.Response.ServerError}, file: {entity.Name}");
                    throw new ConcurrencyException(id);
                }
            }
        }
    }
}
