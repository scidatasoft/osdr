using Elasticsearch.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using Nest;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReIndexing
{
    public class OsdrReindexer
    {
        public class Properties
        {
            public IEnumerable<Field> Fields { get; set; }
        }

        IMongoDatabase _database;
        IElasticClient _elasticClient;
        IBlobStorage _blobStorage;
        
        (string index, string type)[] _indices =  {("files", "file"), ("folders", "folder"), ("records", "record") };
        readonly string[] _indexedBlobs = new[] { ".pdf", ".txt", ".csv", ".tsv", ".xls", ".doc", ".xlx", ".docx" };

        public OsdrReindexer(IMongoDatabase database, IElasticClient elasticClient, IBlobStorage blobStorage)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _elasticClient = elasticClient ?? throw new ArgumentNullException(nameof(elasticClient));
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));

            _indices.AsParallel().ForAll(t =>
                _database.GetCollection<BsonDocument>(t.index.ToPascalCase()).Indexes.CreateOneAsync("{OwnedBy:1, IsDeleted:1}").Wait());
        }

        public void Reindex(IEnumerable<string> users)
        {
            if (users.Select(i => i.ToLower()).Contains("all"))
                users = GetUsers();

            Trace.TraceInformation($"{users.Count()} users found...");

            users.AsParallel()
                .ForAll(i => ReindexAllUserData(i));
        }

        private void ReindexAllUserData(string userId)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            DeleteUserDataFromIndex(userId);

            _indices.AsParallel().ForAll(i => ReindexUserData(i.index, i.type, userId).Wait());
            CreateAliases(userId);

            stopwatch.Stop();
            Trace.TraceInformation($"Content re-indexed, time elapsed: {stopwatch.Elapsed}");
        }

        private void CreateAliases(string userId)
        {
            var alias = _elasticClient.AliasExists(new AliasExistsDescriptor().Name(userId));
            if (alias.Exists)
            {
                _elasticClient.Alias(a => a
                    .Remove(r => r.Index("records").Alias(userId))
                    .Remove(r => r.Index("folders").Alias(userId))
                    .Remove(r => r.Index("files").Alias(userId)));
            }

            try
            {
                _elasticClient.Alias(a => a
                    .Add(add => add.Index("files").Alias(userId)
                        .Filter<dynamic>(qc => qc.Bool(b => b.Should(s => s.Term("OwnedBy", userId), s => s.Term("Node.AccessPermissions.IsPublic", true)))))
                    .Add(add => add.Index("folders").Alias(userId)
                        .Filter<dynamic>(qc => qc.Bool(b => b.Should(s => s.Term("OwnedBy", userId), s => s.Term("Node.AccessPermissions.IsPublic", true)))))
                    .Add(add => add.Index("records").Alias(userId)
                        .Filter<dynamic>(qc => qc.Bool(b => b.Should(s => s.Term("OwnedBy", userId), s => s.Term("Node.AccessPermissions.IsPublic", true)))))
                  );


            }
            catch (ElasticsearchClientException e)
            {
                Trace.TraceError($"Creating alias server error: {e.Response}");
            }
        }

        private void DeleteUserDataFromIndex(string userId)
        {
            _indices.AsParallel()
                .ForAll(i => _elasticClient.DeleteByQuery(new DeleteByQueryRequest(i.index) { Query = new TermQuery { Field = "OwnedBy", Value = userId } }));
        }

        private async Task ReindexUserData(string index, string type, string ownerId)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));

            if (!_indices.Select(i=>i.type).Contains(type))
                throw new ArgumentException(nameof(type));

            Trace.TraceInformation($"Starting re-indexing {type}s for user {ownerId}...");

            await CreateIndex(index, type);
            var entitites = _database.GetCollection<BsonDocument>($"{type.ToPascalCase()}s");
            var nodes = _database.GetCollection<BsonDocument>($"Nodes");
            var filter = new BsonDocument("OwnedBy", new Guid(ownerId))
                .Add("IsDeleted", new BsonDocument("$ne", true));

            try
            {
                using (var cursor = await entitites.Aggregate(new AggregateOptions { BatchSize = 1000 }).Match(filter).Lookup("Nodes", "_id", "_id", "Node")
                    .GraphLookup(nodes, "ParentId", "_id", "$_id", "Parents")
                    .Match(new BsonDocument("$or", new BsonArray(
                    new BsonDocument[] {
                        new BsonDocument("Parents", new BsonDocument("$size", 0)),
                        new BsonDocument("Parents.IsDeleted", new BsonDocument("$ne", true))
                    })))
                    .Lookup<BsonDocument, BsonDocument>("AccessPermissions", "_id", "_id", "AccessPermissions")
                    .Unwind("AccessPermissions", new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
                    .Project("{Parents:0}")
                    .Unwind<dynamic>("Node").ToCursorAsync())
                {
                    while (await cursor.MoveNextAsync())
                    {
                        var batch = cursor.Current;
                        Trace.TraceInformation($"Re-indexing {batch.Count()} {type}s for user {ownerId}...");

                        var bulkDescriptor = new BulkDescriptor();
                        var bulkOperations = new List<IBulkOperation>();

                        foreach (var document in batch)
                        {
                            Guid id = (Guid)((IDictionary<string, object>)document)["_id"];
                            var entity = SyncToApiFormat(document as IDictionary<string, object>);
                            if (type == "file" && _indexedBlobs.Contains(Path.GetExtension((string)document.Name)))
                            {
                                using (var ms = new MemoryStream())
                                {
                                    await _blobStorage.DownloadFileToStreamAsync(entity.Blob.id, ms, entity.Blob.Bucket);
                                    entity.Blob.Base64Content = Convert.ToBase64String(ms.ToArray());
                                }
                                bulkOperations.Add(new BulkIndexOperation<object>(entity) { Id = id.ToString(), Pipeline = "process_blob" });
                            }
                            else
                                bulkOperations.Add(new BulkIndexOperation<object>(entity) { Id = id.ToString() });
                        }
                        if (bulkOperations.Count() > 0)
                        {
                            var bulkRequest = new BulkRequest(index, type)
                            {
                                Operations = bulkOperations
                            };
                            var res = await _elasticClient.BulkAsync(bulkRequest);
                            if (res.IsValid)
                                Trace.TraceInformation($"{batch.Count()} {type}s for user {ownerId} done.");
                            else
                                Trace.TraceError($"Re-indexing {type}s error: {res.ServerError.Error.Reason}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        private IEnumerable<string> GetUsers()
        {
            var collection = _database.GetCollection<BsonDocument>("Users");

            return collection.Find(new BsonDocument())
                .Project("{_id:1}")
                .ToList()
                .Select(d => d["_id"].AsGuid.ToString());
        }

        private dynamic SyncToApiFormat(IDictionary<string, object> entity)
        {
            dynamic result = new ExpandoObject();

            foreach (var key in entity.Keys)
            {
                var value = entity[key];

                if (value is ExpandoObject)
                {
                    var newValue = SyncToApiFormat((IDictionary<string, object>)value);
                    ((IDictionary<string, object>)result).Add(key, newValue);
                }
                else if (value is IList<object>)
                {
                    var newValue = DereferenceListOfObjects((IList<object>)value);
                    ((IDictionary<string, object>)result).Add(key, newValue);
                }
                else
                {
                    ((IDictionary<string, object>)result).Add(key, value);
                }
            }

            if (((IDictionary<string, object>)result).ContainsKey("_id"))
            {
                var id = entity["_id"];
                ((IDictionary<string, object>)result).Remove("_id");
                ((IDictionary<string, object>)result).Add("id", id);
            }

            if (entity.ContainsKey("AccessPermissions"))
            {
                dynamic accessPermissions = result.AccessPermissions;
                ((IDictionary<string, object>)result.Node).Add("AccessPermissions", result.AccessPermissions);
                ((IDictionary<string, object>)result).Remove("AccessPermissions");
            }

            return result;
        }

        private IList<object> DereferenceListOfObjects(IList<dynamic> entities)
        {
            var result = new List<object>();
            foreach (var item in entities)
            {
                if (item is ExpandoObject)
                {
                    var newValue = SyncToApiFormat((IDictionary<string, object>)item);
                    result.Add(newValue);
                }
                else if (item is IList<object>)
                {
                    var newValue = DereferenceListOfObjects((IList<object>)item);
                    ((IList<object>)result).Add(newValue);
                }
                else
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private async Task CreateIndex(string indexName, string typeName)
        {
            var result = await _elasticClient.IndexExistsAsync(indexName);
            if (!result.Exists)
            {
                ICreateIndexRequest createIndexRequest;
                switch (indexName)
                {
                    case "records":
                        createIndexRequest = new CreateIndexDescriptor(indexName)
                            .Mappings(s => s
                                .Map(typeName, tm => tm
                                    .Properties(p => p.Keyword(k => k.Name("OwnedBy")))
                                    .Properties(p => p.Keyword(k => k.Name("FileId")))
                                    .Properties(p => p.Object<dynamic>(f => f.Name("Properties")
                                        .Properties(np => np.Object<dynamic>(t => t.Name("ChemicalProperties")
                                            .Properties(cp => cp.Text(tp => tp.Name("Value")))))))));
                        break;

                    default:
                        createIndexRequest = new CreateIndexDescriptor(indexName)
                            .Mappings(s => s
                                .Map(typeName, tm => tm
                                    .Properties(p => p.Keyword(k => k.Name("OwnedBy")))));
                        break;
                }

                try
                {
                    await _elasticClient.CreateIndexAsync(createIndexRequest);
                    Trace.TraceInformation($"Created mapping for index: '{indexName}', type: '{typeName}'");
                }
                catch (ElasticsearchClientException e)
                {
                    Trace.TraceError($"Creating mapping server error: {e.Response.DebugInformation}");
                }
            }
        }
    }
}
