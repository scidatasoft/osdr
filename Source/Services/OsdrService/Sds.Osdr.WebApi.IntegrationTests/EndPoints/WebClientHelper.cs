using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.IntegrationTests.EndPoints
{
    public class OsdrWebClient
    {
        private HttpClient client;
        private Uri BaseUri { get; }

        public OsdrWebClient(HttpClient client)
        {
            this.client = client;
            BaseUri = new Uri("http://localhost:28611");
        }

        public async Task<HttpResponseMessage> GetData(string url, string contentType = "application/json")
        {
            var request = new HttpRequestMessage(new HttpMethod("GET"), new Uri(BaseUri, url));

            request.Content = new StringContent("");

            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            var response = await client.SendAsync(request);

            return  response;
        }
        public async Task<HttpResponseMessage> PatchData(string url, string stringContent)
        {
            var content = new StringContent(stringContent);

            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), new Uri(BaseUri, url))
            {
                Content = content
            };

            HttpResponseMessage response = new HttpResponseMessage();

            response = await client.SendAsync(request);

            return response;
        }
		
        public async Task<HttpResponseMessage> PostData(string url, string data)
        {
            var httpContent = new StringContent(data);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(BaseUri, url), httpContent);

            return  response;
        }
    }
}