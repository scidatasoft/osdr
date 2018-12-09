using CQRSlite.Domain.Exception;
using Elasticsearch.Net;
using MassTransit;
using MongoDB.Driver;
using Nest;
using Sds.Indexing.Extensions;
using Sds.Osdr.Generic.Domain.Events.Files;
using Sds.Storage.Blob.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Indexing.EventHandlers
{
    public class FileEventHandler : BaseEntityEventHandler, IConsumer<FilePersisted>, IConsumer<NodeStatusPersisted>, IConsumer<FileDeleted>,
                                    IConsumer<FileNamePersisted>, IConsumer<FileParentPersisted>, IConsumer<PermissionsChanged>
    {
        const string typeName = "file";
        const string indexName = "files";
        readonly string[] indexedBlobs = new[] { ".pdf", ".txt", ".csv", ".tsv", ".xls", ".doc", ".xlx", ".docx" };
        IBlobStorage _blobStorage;

        public FileEventHandler(IElasticClient elasticClient, IMongoDatabase database, IBlobStorage blobStorage)
            : base(elasticClient, database, $"{typeName.ToPascalCase()}s")
        {
            _blobStorage = blobStorage;
        }

        public async Task Consume(ConsumeContext<FileDeleted> context)
        {
            await RemoveEntityAsync(indexName, typeName, context.Message.Id);
            await _elasticClient.DeleteByQueryAsync(new DeleteByQueryRequest("records") { Query = new TermQuery { Field = "FileId", Value = context.Message.Id } });
        }

        public async Task Consume(ConsumeContext<FileParentPersisted> context)
        {
            await UpdateFileDocument(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<FileNamePersisted> context)
        {
            await UpdateFileDocument(context.Message.Id);
        }

        public async Task Consume(ConsumeContext<FilePersisted> context)
        {
            if (indexedBlobs.Contains(Path.GetExtension(context.Message.FileName)))
                await CreateFileDocument(context.Message.Id);
            else
                await IndexEntityAsync(indexName, typeName, context.Message.Id);
        }

        public async Task Consume(ConsumeContext<NodeStatusPersisted> context)
        {
            if (context.Message.Status == Osdr.Generic.Domain.FileStatus.Processed)
                await UpdateFileDocument(context.Message.Id);
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

        private async Task UpdateFileDocument(Guid id)
        {
            dynamic entity = await GetEntityFromDatabase(id);
            bool entityIsDeleted = ((IDictionary<string, object>)entity).ContainsKey("IsDeleted") ? entity.IsDeleted : false;

            if (!entityIsDeleted)
            {
                try
                {
                    if (indexedBlobs.Contains(Path.GetExtension((string)entity.Name)))
                        await _elasticClient.UpdateAsync<object, object>(new UpdateRequest<object, object>(indexName, typeName, id) { Doc = entity });
                    else
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
