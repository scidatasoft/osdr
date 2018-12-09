using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sds.Osdr.WebApi.Models;
using Sds.Storage.Blob.Core;

namespace Sds.Osdr.WebApi.DataProviders
{
    public interface IOrganizeDataProvider
    {
        IEnumerable<BaseNode> GetBreadcrumbs(Guid itemId);
        PagedList<dynamic> GetSharedNodes(Guid userId, int pageNumber = 1, int pageSize = 20, bool isPublicOnly = false);
        bool IsItemAccessible(Guid itemId, Guid? userId = null);
        Task<IBlob> GetEntityImageAsync(string entityType, Guid itemId, Guid imageId);
        Task<(IBlob blob, string osdrFileName)> GetEntityBlobAsync(string entityType, Guid itemId, Guid blobId);
        Task<object> GetInfoboxMetadataAsync(string entityType, Guid entityId, string infoboxType);
        Task<object> GetEntityMetadataAsync(string entityType, Guid entityId);
    }
}