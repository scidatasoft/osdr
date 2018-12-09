using Sds.Osdr.WebApi.IntegrationTests.EndPoints;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.IntegrationTests.Extensions
{
    public static class OsdrWebClientEntitiesExtensions
    {
        public static async Task<HttpResponseMessage> DeleteFolder(this OsdrWebClient client, Guid id, int version)
        {
            return await client.PatchData("api/nodecollections", $@"[{{'op': 'add','path':'/deleted','value':[{{'id':'{id}','version':{version},'type':'Folder'}}]}}]");
        }

        public static async Task<HttpResponseMessage> MoveFolder(this OsdrWebClient client, Guid idFolder, int versionFolder, Guid folderTo)
        {
            return await client.PatchData($"api/nodecollections", 
                $"[{{'op':'add','path':'/moved','value':[{{'id':'{idFolder}','version':{versionFolder},'type':'Folder'}}]}},{{'op':'replace','path':'/parentid','value':'{folderTo}'}}]");
        }

        public static async Task<HttpResponseMessage> GetOSDRVersion(this OsdrWebClient client)
        {
            return await client.GetData("/api/version");
        }

        public static async Task<HttpResponseMessage> GetUserMe(this OsdrWebClient client)
        {
            var response = await client.GetData("api/me");
            return response;
        }

        public static async Task<HttpResponseMessage> GetUserById(this OsdrWebClient client, Guid userId)
        {
            var response = await client.GetData($"api/users/{userId}");
            return response;
        }

        public static async Task<HttpResponseMessage> GetUserPublicInfoById(this OsdrWebClient client, Guid userId)
        {
            var response = await client.GetData($"api/users/{userId}/public-info");
            return response;
        }
    }
}