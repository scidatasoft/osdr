using MongoDB.Bson;
using MongoDB.Driver;
using Nest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sds.Indexing.Extensions;
using Elasticsearch.Net;
using Serilog;
using System.Dynamic;
using Sds.Osdr.Generic.Domain.ValueObjects;

namespace Sds.Indexing.EventHandlers
{
    public abstract class BaseEntityEventHandler
    {
        readonly IMongoDatabase _database;
        readonly IMongoCollection<dynamic> _mongoDBCollection;
        protected readonly IElasticClient _elasticClient;
        
        protected readonly IMongoCollection<dynamic> _nodesMongoDbCollection;

        public BaseEntityEventHandler(IElasticClient elasticClient, IMongoDatabase database, string mongoDBCollection)
        {
            _elasticClient = elasticClient ?? throw new ArgumentNullException(nameof(elasticClient));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _mongoDBCollection = _database.GetCollection<dynamic>(mongoDBCollection?.ToPascalCase() ?? throw new ArgumentNullException(nameof(mongoDBCollection)));
            _nodesMongoDbCollection = _database.GetCollection<dynamic>("Nodes");

        }

        public async Task IndexEntityAsync(string indexName, string typeName, Guid id)
        {
            
            dynamic entity = await GetEntityFromDatabase(id);
            try
            {
                var indexingResult = await _elasticClient.IndexAsync<object>(new IndexRequest<object>(entity, indexName, typeName, id));

                Guid userId = entity.OwnedBy;

                var alias = await _elasticClient.AliasExistsAsync(new AliasExistsDescriptor().Name(userId.ToString()).Index(Indices.Parse(indexName)));

                if (!alias.Exists)
                {
                    await _elasticClient.AliasAsync(a => a
                    .Add(add => add.Index(indexName).Alias(userId.ToString())
                        .Filter<dynamic>(qc => qc.Bool(b => b.Should(s => s.Term("OwnedBy", userId.ToString()), s => s.Term("IsPublic", true))))));
                }

            }
            catch (ElasticsearchClientException e)
            {
                Log.Error($"{e.Response.ServerError}");
            }
        }

        protected async Task SetPermissions(string indexName, string typeName, string entityId, AccessPermissions accessPermissions)
        {
            await _elasticClient.UpdateAsync<object, object>(new UpdateRequest<object, object>(indexName, typeName, entityId)
            {
                Doc = new { Node = new { AccessPermissions = accessPermissions } }
            });
        }

        protected async Task<dynamic> GetEntityFromDatabase(Guid id)
        {
            var entity = await _mongoDBCollection.Find(new BsonDocument("_id", id))
                .FirstAsync();

            var node = await _nodesMongoDbCollection.Aggregate().Match(new BsonDocument("_id", id))
                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "_id", "_id", nameof(AccessPermissions))
                .Unwind(nameof(AccessPermissions), new AggregateUnwindOptions<dynamic> { PreserveNullAndEmptyArrays = true })
                .FirstOrDefaultAsync();
            if (node != null)
                ((IDictionary<string, object>)entity).Add("Node", node);

            entity = SyncToApiFormat(entity);

            return entity;
        }

        public async Task RemoveEntityAsync(string indexName, string typeName, Guid id)
        {
            await _elasticClient.DeleteAsync(new DeleteRequest(indexName, typeName, id));
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
    }
}
