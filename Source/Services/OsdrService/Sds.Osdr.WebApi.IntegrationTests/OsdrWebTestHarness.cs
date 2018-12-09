//using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.WebApi.IntegrationTests.EndPoints;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class Token
    {
        public string access_token { get; set; }
    }

    public class UserInfo
    {
        public Guid sub { get; set; }
        public string email { get; set; }
        public string name { get; set; }
        public string preferred_username { get; set; }
        public string given_name { get; set; }
        public string family_name { get; set; }
    }

    public class KeyCloalClient : HttpClient
    {
        public Uri Authority { get; } = new Uri("http://keycloak:8080/auth/realms/OSDR/");
        public string ClientId { get; } = "osdr_webapi";
        public string ClientSecret { get; } = "52f5b3fc-2167-40b1-9508-6bb3091782bd";

        public KeyCloalClient() : base()
        {
        }

        public async Task<Token> GetToken(string username, string password)
        {
            var nvc = new List<KeyValuePair<string, string>>();
            nvc.Add(new KeyValuePair<string, string>("username", username));
            nvc.Add(new KeyValuePair<string, string>("password", password));
            nvc.Add(new KeyValuePair<string, string>("grant_type", "password"));

            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Authority, "protocol/openid-connect/token")) { Content = new FormUrlEncodedContent(nvc) };

            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));

            var json = await SendAsync(request).Result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Token>(json);
        }

        public async Task<UserInfo> GetUserInfo(string username, string password)
        {
            var token = await GetToken(username, password);

            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);

            var userInfoResponse = await GetAsync(new Uri(Authority, "protocol/openid-connect/userinfo"));

            var json = await userInfoResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<UserInfo>(json);
        }
    }

    public class OsdrWebTestHarness : OsdrTestHarness
    {
        private HttpClient JohnClient { get; }
        private HttpClient JaneClient { get; }
        private HttpClient UnauthorizedClient { get; }

        public OsdrWebClient JohnApi { get; }
        public OsdrWebClient JaneApi { get; }
        public OsdrWebClient UnauthorizedApi { get; }

        private KeyCloalClient keycloak = new KeyCloalClient();

        public OsdrWebTestHarness() : base()
        {
            var token = keycloak.GetToken("john", "qqq123").Result;

            JohnClient = new HttpClient();
            JohnClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);

            token = keycloak.GetToken("jane", "qqq123").Result;

            JaneClient = new HttpClient();
            JaneClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);

            UnauthorizedClient = new HttpClient();

            JohnApi = new OsdrWebClient(JohnClient);
            JaneApi = new OsdrWebClient(JaneClient);
            UnauthorizedApi = new OsdrWebClient(UnauthorizedClient);
        }

        protected override void Seed()
        {
            var john = keycloak.GetUserInfo("john", "qqq123").Result;

            JohnId = john.sub;

            try
            {
                var johnUser = Session.Get<User>(JohnId).Result;
            }
            catch (AggregateException)
            {
                this.CreateUser(JohnId, "John Doe", "John", "Doe", "john", "john@your-company.com", null, JohnId).Wait();
            }

            var jane = keycloak.GetUserInfo("jane", "qqq123").Result;

            JaneId = jane.sub;

            try
            {
                var janeUser = Session.Get<User>(JaneId).Result;
            }
            catch (AggregateException)
            {
                this.CreateUser(JaneId, "Jane Doe", "Jane", "Doe", "jane", "jane@your-company.com", null, JaneId).Wait();
            }
        }

        public override void Dispose()
        {
            JohnClient.Dispose();
            JaneClient.Dispose();
            UnauthorizedClient.Dispose();

            base.Dispose();
        }
    }
}