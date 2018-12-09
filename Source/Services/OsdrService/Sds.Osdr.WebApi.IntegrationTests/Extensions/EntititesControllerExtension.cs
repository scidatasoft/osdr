using System;
using System.Net.Http;
using System.Threading.Tasks;
using Sds.Osdr.WebApi.IntegrationTests.EndPoints;

namespace Sds.Osdr.WebApi.IntegrationTests.Extensions
{
    public static class EntititesControllerExtension
    {
        public static async Task<HttpResponseMessage> GetEntityById(this OsdrWebClient client, string type, Guid id)
        {
            return await client.GetData($"/api/entities/{type}/{id}");
        }
        
        public static async Task<HttpResponseMessage> GetEntitiesMe(this OsdrWebClient client, string type)
        {
            return await client.GetData($"api/entities/{type}/me");
        }
        
        public static async Task<HttpResponseMessage> GetFileEntityById(this OsdrWebClient client, Guid id)
        {
            return await client.GetEntityById("Files", id);
        }

        public static async Task<HttpResponseMessage> GetModelEntityById(this OsdrWebClient client, Guid id)
        {
            return await client.GetEntityById("Models", id);
        }

        public static async Task<HttpResponseMessage> GetNodeEntityById(this OsdrWebClient client, Guid id)
        {
            return await client.GetEntityById("Nodes", id);
        }

        public static async Task<HttpResponseMessage> GetFolderEntityById(this OsdrWebClient client, Guid id)
        {
            return await client.GetEntityById("Folders", id);
        }
        
        public static async Task<HttpResponseMessage> GetRecordEntityById(this OsdrWebClient client, Guid id)
        {
            return await client.GetData($"/api/entities/records/{id}");
        }

        public static async Task<HttpResponseMessage> CreateFolderEntity(this OsdrWebClient client, Guid parentNodeId, string name)
        {
            return await client.PostData("api/entities/folders", $"{{'Name': '{name}', parentId: '{parentNodeId}'}}");
        }
        
        public static async Task<HttpResponseMessage> GetFolder(this OsdrWebClient client, Guid id)
        {
            return await client.GetData($"api/entities/folders/{id}");
        }
        
        public static async Task<HttpResponseMessage> RenameFolder(this OsdrWebClient client, Guid id, string newName)
        {
            return await client.PatchData($"api/entities/folders/{id}?version=1", $"[{{op: 'replace', path: '/name', value: '{newName}'}}]");
        }

        public static async Task<HttpResponseMessage> GetEntitiesFolders(this OsdrWebClient client)
        {
            return await client.GetData("api/entities/folders/me");
        }
        
        public static async Task<HttpResponseMessage> GetUserEntityById(this OsdrWebClient client, Guid userId)
        {
            return await GetEntityById(client, "users", userId);
        }
        
        public static async Task<HttpResponseMessage> SetPublicFileEntity(this OsdrWebClient client, Guid fileId, int version, bool isPublic)
        {
            var url = $"/api/entities/files/{fileId}?version={version}";
            var data = $"[{{'op':'replace','path':'/Permissions/IsPublic','value':{isPublic.ToString().ToLower()}}}]";
 
            return await client.PatchData(url, data);
        }
        
        public static async Task<HttpResponseMessage> SetPublicFoldersEntity(this OsdrWebClient client, 
            Guid folderId, int version, bool isPublic)
        {
            var url = $"/api/entities/folders/{folderId}";
            var data = $"[{{'op':'replace','path':'/Permissions/IsPublic','value':{isPublic.ToString().ToLower()}}}]";
 
            return await client.PatchData(url, data);
        }
        
        public static async Task<HttpResponseMessage> SetPublicModelsEntity(this OsdrWebClient client, 
            Guid modelId, bool isPublic)
        {
            var url = $"/api/entities/models/{modelId}";
            var data = $"[{{'op':'replace','path':'/Permissions/IsPublic','value':{isPublic.ToString().ToLower()}}}]";
 
            return await client.PatchData(url, data);
        }
        
        public static async Task<HttpResponseMessage> GetPublicEntity(this OsdrWebClient client, string type, 
            int pageNumber = 1, int pageSize = 20, string filter = null, string projection = null)
        {
            HttpResponseMessage response = null;
            var stringProjection = "";

            if (projection != null)
                stringProjection = $"&$projection={projection}";
            
            if (filter != null)
                response = await client.GetData($"/api/entities/{type}/public?PageNumber={pageNumber}&PageSize={pageSize}&$filter={filter}{stringProjection}");
            else response = await client.GetData($"/api/entities/{type}/public?PageNumber={pageNumber}&PageSize={pageSize}&{stringProjection}");

            return response;
        }
        
        public static async Task<HttpResponseMessage> GetEntityStreamById(this OsdrWebClient client, string type, Guid id)
        {
            return await client.GetData($"/api/entities/{type}/{id}/stream");
        }

        public static async Task<HttpResponseMessage> GetStreamById(this OsdrWebClient client, string type, Guid id, int start = 0, int count = -1)
        {
            return await client.GetData($"/api/entities/{type}/{id}/stream?Start={start}&Count={count}");
        }

        public static async Task<HttpResponseMessage> GetStreamFileEntityById(this OsdrWebClient client, Guid fileId, int start = 0, int count = -1)
        {
            return await GetStreamById(client, "files", fileId, start, count);
        }

        public static async Task<HttpResponseMessage> GetStreamRecordEntityById(this OsdrWebClient client, Guid recordId, int start = 0, int count = -1)
        {
            return await GetStreamById(client, "records", recordId, start, count);
        }

        public static async Task<HttpResponseMessage> GetStreamFolderEntityById(this OsdrWebClient client, Guid folderId, int start = 1, int count = -1)
        {
            return await GetStreamById(client, "folders", folderId, start, count);
        }

        public static async Task<HttpResponseMessage> GetImagesById(this OsdrWebClient client, string type, Guid id, Guid imageId)
        {
            return await client.GetData($"/api/entities/{type}/{id}/images/{imageId}");
        }

        public static async Task<HttpResponseMessage> GetImagesFileEntityById(this OsdrWebClient client, Guid fileId, Guid imageId)
        {
            return await GetImagesById(client, "files", fileId, imageId);
        }

        public static async Task<HttpResponseMessage> GetImagesRecordEntityById(this OsdrWebClient client, Guid recordId, Guid imageId)
        {
            return await GetImagesById(client, "records", recordId, imageId);
        }

        public static async Task<HttpResponseMessage> GetBlobById(this OsdrWebClient client, string type, Guid id, Guid blobId)
        {
            return await client.GetData($"/api/entities/{type}/{id}/blobs/{blobId}");
        }

        public static async Task<HttpResponseMessage> GetBlobFileEntityById(this OsdrWebClient client, Guid fileId, Guid blobId)
        {
            return await GetBlobById(client, "files", fileId, blobId);
        }

        public static async Task<HttpResponseMessage> GetBlobFileMetadataById(this OsdrWebClient client, Guid fileId, Guid blobId)
        {
            return await client.GetData($"/api/entities/files/{fileId}/blobs/{blobId}/info");
        }

        public static async Task<HttpResponseMessage> GetBlobRecordEntityById(this OsdrWebClient client, Guid recordId, Guid blobId)
        {
            return await GetBlobById(client, "records", recordId, blobId);
        }
    }
}