using System;
using System.Collections.Generic;
using System.Text;
using Elasticsearch.Net;

namespace Sds.NotificationService.Processing.Monitoring
{
    public class RepositoryServices
    {
        public string[] CheckingServices { get; }
        public List<Service> NoUpService { get; }
        private ElasticLowLevelClient _client;
        
        public RepositoryServices(ElasticLowLevelClient client, string[] checkingServices)
        {
            _client = client;
            CheckingServices = checkingServices;
            NoUpService = new List<Service>();
        }
        
        public void Update()
        {
            NoUpService.Clear();
            
            foreach(var serviceName in CheckingServices)
            {
                var nowDate = DateTime.Now.ToString("o");
                var dateString = nowDate.Substring(0, nowDate.IndexOf("T")).Replace("-", ".");
                var esQuery = $@"{{
                ""_source"": [
                ""@timestamp"",
                ""error"",
                ""host"",
                ""monitor"",
                ""type"",
                ""up""
                    ],
                ""query"": {{
                    ""match"": {{
                        ""type"": ""{serviceName}""
                    }}
                }},
                ""sort"": [
                {{
                    ""@timestamp"": ""desc""
                }}],
                ""size"": 1
            }}";
//                var queryJson = Encoding.UTF8.GetString(Resources.elasticsearch_query).Replace("{QUERY_TYPE}", serviceName);
                var heartbeat = $"heartbeat-{dateString}";
                var json = Query(heartbeat, "doc", esQuery);
                
                if (json != null)
                {
                    var service = new Service(json);

                    if (!service.Up)
                        NoUpService.Add(service);
                }
            }
        }
        
        private string Query(string index, string type, string query)
        {
            ElasticsearchResponse<string> result = _client.Search<string>(index, type, query);
            var body = result.Body;
            
            return body;
        }
    }
}