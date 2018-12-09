using System;
using System.Net.Http;
using System.Threading.Tasks;
using Sds.Osdr.WebApi.IntegrationTests.EndPoints;

namespace Sds.Osdr.WebApi.IntegrationTests.Extensions
{
    public static class NodesControllerExtension
    {
        public static async Task<HttpResponseMessage> GetPublicNodes(this OsdrWebClient client, int pageNumber = 1, 
            int pageSize = 20, string filter = null, string projection = null)
        {
            HttpResponseMessage response = null;
            var stringProjection = "";

            if (projection != null)
                stringProjection = $"&$projection={projection}";
            
            if (filter != null)
                response = await client.GetData($"/api/nodes/public?PageNumber={pageNumber}&PageSize={pageSize}&$filter={filter}{stringProjection}");
            else response = await client.GetData($"/api/nodes/public?PageNumber={pageNumber}&PageSize={pageSize}{stringProjection}");

            return response;
        }
        
        public static async Task<HttpResponseMessage> GetPublicNodes(this OsdrWebClient client, Guid id)
        {
            return await client.GetData($"/api/nodes/public?id={id}");
        }
        

        public static async Task<HttpResponseMessage> GetSharedFiles(this OsdrWebClient client)
        {
            return await client.GetData("api/nodes/shared");
        }
        
        public static async Task<HttpResponseMessage> NodesFindByField(this OsdrWebClient client, string fieldName, Guid id)
        {
            return await client.GetData($"api/nodes?$filter={fieldName} eq '{id}'");
        }
        
        public static async Task<HttpResponseMessage> GetNodesMe(this OsdrWebClient client)
        {
            return await client.GetData("api/nodes/me");
        }
        
        public static async Task<HttpResponseMessage> GetNodes(this OsdrWebClient client, string filter = null, string projection = null)
        {
            var stringProjection = "";

            if (projection != null)
                stringProjection = $"&$projection={projection}";
            
            if (filter != null)
                return await client.GetData($"api/nodes?$filter={filter}{stringProjection}");
            
            return await client.GetData($"api/nodes");
        }
        
        public static async Task<HttpResponseMessage> GetNodeById(this OsdrWebClient client, Guid id)
        {
            return await client.GetData($"api/nodes/{id}");
        }
		
        public static async Task<HttpResponseMessage> GetNodesById(this OsdrWebClient client, Guid id)
        {
            return await client.GetData($"/api/nodes/{id}/nodes");
        }

        public static async Task<HttpResponseMessage> GetPageNumber(this OsdrWebClient client, Guid id, int pageSize)
        {
            return await client.GetData($"api/nodes/{id}/page?pageSize={pageSize}");
        }
    }
}