using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.WebApi.Extensions;
using Sds.Osdr.WebApi.Models;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.DataProviders
{
    public class OrganizeDataProvider : IOrganizeDataProvider
    {
        readonly IMongoDatabase _database;
        readonly IMongoCollection<BsonDocument> _nodes;
        readonly IBlobStorage _blobStorage;

        readonly FilterDefinition<BsonDocument> _filterBase = Builders<BsonDocument>.Filter.Ne("IsDeleted", true);
        readonly FilterDefinitionBuilder<BsonDocument> _builder = Builders<BsonDocument>.Filter;

        //BsonDocument _filter = new BsonDocument("IsDeleted", new BsonDocument("$ne", true));
        

        public OrganizeDataProvider(IMongoDatabase database, IBlobStorage blobStorage)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _nodes = _database.GetCollection<BsonDocument>("Nodes");
            _blobStorage = blobStorage;
        }

        private IMongoCollection<BsonDocument> GetEntitiesCollection(string entityType)
        {
            return _database.GetCollection<BsonDocument>(entityType);
        }

        public IEnumerable<BaseNode> GetBreadcrumbs(Guid itemId)
        {
            var filter = _filterBase & _builder.Eq("_id", itemId);

            var parents = _nodes.Aggregate().Match(filter)
                .GraphLookup(_nodes, "ParentId", "_id", "$ParentId", "Parent")
                .Unwind("Parent")
                .ReplaceRoot<BaseNode>("$Parent")
                .ToList();

            if (parents.Count() > 0)
            {
                var result = new List<BaseNode>();
                var it = parents.First(p => p.Type == "User");
                do
                {
                    result.Add(it);
                    it = parents.FirstOrDefault(p => p.ParentId == it.Id);

                } while (it != null);

                return result;
            }
            else
            {
                return parents;
            }
        }

        public bool IsItemAccessible(Guid itemId, Guid? userId = null)
        {
            if (IsItemExistAndHasPermissions(itemId, userId))
                return true;

            var breadCrumbs = GetBreadcrumbs(itemId);
            foreach (var item in breadCrumbs)
                if (IsItemExistAndHasPermissions(item.Id, userId))
                    return true;

            return false;
        }

        private bool IsItemExistAndHasPermissions(Guid itemId, Guid? userId = null)
        {
            var filter = _filterBase & _builder.Eq("_id", itemId);

            var permissions = _nodes.Aggregate().Match(filter)
                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "_id", "_id", nameof(AccessPermissions))
                .Unwind(nameof(AccessPermissions), new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
                .Project("{AccessPermissions:1, OwnedBy:1}")
                .FirstOrDefault();

            if (permissions != null)
            {
                if (permissions.Contains("AccessPermissions") && (permissions["AccessPermissions"]["IsPublic"].AsNullableBoolean ?? false))
                    return true;

                if (permissions.Contains("OwnedBy") && (permissions["OwnedBy"].AsGuid == userId))
                    return true;

                if (permissions["_id"].AsGuid == userId)
                    return true;

                return false;
            }
            else
                return false;
        }

        public PagedList<dynamic> GetSharedNodes(Guid userId, int pageNumber = 1, int pageSize = 20, bool isPublicOnly = false)
        {
            var filter = _filterBase & _builder.Eq("OwnedBy", userId) & _builder.Or(
                _builder.Eq("AccessPermissions.IsPublic", true), 
                _builder.Not(_builder.Size("AccessPermissions.Users",0)), 
                _builder.Not(_builder.Size("AccessPermissions.Groups",0))
                );

            if (isPublicOnly)
                filter = filter & _builder.Eq("AccessPermissions.IsPublic", true);

            var nodes = _nodes.Aggregate()
                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "_id", "_id", nameof(AccessPermissions))
                .Unwind<dynamic>(nameof(AccessPermissions))
                .Match(filter.Render(_nodes.DocumentSerializer, _nodes.Settings.SerializerRegistry));

            return PagedList(nodes, pageNumber, pageSize);
        }

        private PagedList<TProjection> PagedList<TProjection>(IAggregateFluent<TProjection> findFluent, int pageNumber, int pageSize)
        {
            var count = findFluent.Any() ? findFluent.Group(new BsonDocument
            {
                { "_id", "_id" },
                {"count", new BsonDocument("$sum", 1)}
            })
            .FirstAsync().Result["count"].AsInt32 : 0;

            var items = findFluent.Skip(pageSize * (pageNumber - 1))
                    .Limit(pageSize)
                    .ToList();

            return new PagedList<TProjection>(items, count, pageNumber, pageSize);
        }

        public async Task<IBlob> GetEntityImageAsync(string itemType, Guid itemId, Guid imageId)
        {
            var filter = _filterBase & _builder.Eq("_id", itemId) & _builder.Eq("Images._id", imageId);

            var imageInfo = _database.GetCollection<BsonDocument>(itemType.ToPascalCase()).Aggregate().Match(filter)
               .Project("{Images:1}")
               .Unwind("Images")
               .ReplaceRoot<BsonDocument>("$Images")
               .FirstOrDefault();

            if (imageInfo != null)
                return await _blobStorage.GetFileAsync(imageId, imageInfo["Bucket"].AsString);
            else
                return null;
        }

        public async Task<(IBlob blob, string osdrFileName)> GetEntityBlobAsync(string itemType, Guid itemId, Guid blobId)
        {
            var filter = _filterBase & _builder.Eq("_id", itemId) & _builder.Or(
                _builder.Eq("Blob._id", blobId),
                _builder.Eq("Pdf.BlobId", blobId)); //TODO: should be refactoring !!!!!

            var blobInfo = _database.GetCollection<BsonDocument>(itemType.ToPascalCase()).Aggregate().Match(filter)
               .Project("{Blob:1, Name:1}")
               .FirstOrDefault();

            return blobInfo != null ? (await _blobStorage.GetFileAsync(blobId, blobInfo["Blob"]["Bucket"].AsString), blobInfo["Name"].AsString) : (null, string.Empty);
        }

        public async Task<object> GetInfoboxMetadataAsync(string entityType, Guid entityId, string infoboxType)
        {
            var filter = new BsonDocument("InfoBoxType", infoboxType);
            if (infoboxType.Equals("fields", StringComparison.InvariantCultureIgnoreCase))
                filter.Add("FileId", entityId);

            return await _database.GetCollection<dynamic>("Metadata").Find(filter).SingleOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<object> GetEntityMetadataAsync(string entityType, Guid entityId)
        {
            var filter = new BsonDocument("_id", entityId);

            return await _database.GetCollection<dynamic>("Metadata").Find(filter).SingleOrDefaultAsync().ConfigureAwait(false);
        }
    }
}
