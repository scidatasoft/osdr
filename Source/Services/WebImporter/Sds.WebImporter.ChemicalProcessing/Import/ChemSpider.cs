using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sds.WebImporter.ChemicalProcessing.CommandHandlers
{
    public class Chemspider : IDisposable
    {
        private string fileId;
        private List<string> searchQueries = new List<string>();
        private List<Guid> quertyIds = new List<Guid>();
        private StringBuilder fileContent = new StringBuilder();
        private JSONClient Client = new JSONClient();
        public string Content { get; }
        public Dictionary<string, object> Meta;

        public Chemspider(IEnumerable<string> urls)
        {
            var ids = ParseUrl(urls);
            if (ids.Any())
            {
                var compounds = Client.GetRecordsAsCompounds(ids);
                AppendToFile(JsonConvert.SerializeObject(compounds.ToList()));
            }

            foreach (var searchQuery in searchQueries)
            {
                var id = Client.SimpleSearch(new SimpleSearchOptions(searchQuery));
                quertyIds.Add(id);
            }

            foreach (var id in quertyIds)
            {
                var count = WaitResponse(id);
                for (int i = 0; i < count; i += 2)
                {
                    var compaunds = Client.GetSearchResultAsCompounds(id, i, 2);
                    AppendToFile(JsonConvert.SerializeObject(compaunds.ToList()));
                }
                if (count > 2 && count % 2 != 0)
                {
                    var compaunds = Client.GetSearchResultAsCompounds(id, count - 1, 2);
                    AppendToFile(JsonConvert.SerializeObject(compaunds.ToList()));
                }
            }
            var url = urls.First();
            var uri = new Uri((url.ToLower().StartsWith("http") ? "" : "http://") + url);
            var splitedHost = uri.Host.Split('.').ToArray();
            var host = (splitedHost.Count() > 2 ? $"{splitedHost[splitedHost.Count() - 2]}.{splitedHost[splitedHost.Count() - 1]}" : uri.Host);

            Meta = new Dictionary<string, object>() {
                { "ImportedFrom", host },
                { "Url", uri.AbsoluteUri },
                { "Source Content Type", "application/json" },
            };

            Content = $"[{fileContent.ToString()}]";
        }

        private void AppendToFile(string json)
        {
            if (json.Length > 0)
            {
                if (fileContent.Length > 9)
                {
                    fileContent.Append(",");
                }
                fileContent.Append(json.Substring(1, json.Length - 2));
            }
        }

        private int WaitResponse(Guid id)
        {
            int limit = 100;
            var count = 0;
            var stoped = false;
            while (!stoped && --limit > 0)
            {
                var status = Client.GetSearchStatus(id);
                if (status.Statue == 0)
                {
                    count = status.Count;
                    stoped = true;
                }
            };
            return count;
        }

        private IEnumerable<int> ParseUrl(IEnumerable<string> urls)
        {
            var ids = new List<int>();
            foreach (var urlItem in urls)
            {
                if (urlItem == null)
                {
                    continue;
                }
                var url = urlItem.ToLower();
                // processing urls as
                //http://www.chemspider.com/Search.aspx?rid=7c4ab79b-c591-4521-8130-e725ce7121d3&page_num=0
                if (url.Contains("chemspider.com/search.aspx?"))
                {
                    if (url.Contains("rid"))
                    {
                        //"http://parts.chemspider.com/JSON.ashx?op=GetSearchResultAsCompounds&rid="
                        var rawIds = url.Split(new string[] { "rid" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var id in rawIds)
                        {
                            if (id.StartsWith("="))
                            {
                                var idLength = id.IndexOf("&");
                                idLength = idLength == -1 ? id.Length : idLength;
                                quertyIds.Add(Guid.Parse(id.Substring(1, idLength - 1)));//
                            }
                        }
                    }
                    if (url.Contains("?q="))
                    {
                        var rawIds = url.Split(new string[] { "q" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var id in rawIds)
                        {
                            if (id.StartsWith("="))
                            {
                                var idLenght = id.IndexOf("&");
                                idLenght = idLenght == -1 ? id.Length : idLenght;
                                searchQueries.Add("http://parts.chemspider.com/JSON.ashx?op=SimpleSearch&searchOptions.QueryText=" + id.Substring(1, idLenght - 1));
                                searchQueries.Add(id.Substring(1, idLenght - 1));
                            }
                        }

                    }
                }
                else
                // processing urls as
                // http://parts.chemspider.com/JSON.ashx?op=SimpleSearch&searchOptions.QueryText=1
                if (url.Contains("searchoptions.querytext"))
                {
                    var query = url.Split("?&".ToArray()).FirstOrDefault(s => s.ToLower().Contains("searchoptions.querytext="));
                    if (query != null)
                    {
                        searchQueries.Add(query.ToLower().Replace("searchoptions.querytext=", ""));

                    }
                }
                else
                // processing urls as 
                // http://parts.chemspider.com/JSON.ashx?op=GetRecordsAsCompounds&csids[0]=1&csids[1]=2&serfilter=Compound[CSID|Name|Mol]
                if (url.Contains("csids"))
                {
                    foreach (var attr in url.Split(new string[] { "csids" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (attr.StartsWith("["))
                        {
                            var id = attr.Substring(attr.IndexOf('=') + 1);
                            var strEnd = id.IndexOf('&');
                            id = id.Substring(0, strEnd == -1 ? id.Length : strEnd);
                            //ids.Add(string.Format("csids[{0}]=", ids.Count) + id);
                            ids.Add(int.Parse(id));
                            if (string.IsNullOrEmpty(fileId))
                            {
                                fileId = id;
                            }
                        }
                    }
                }
                else
                // http://www.chemspider.com/Chemical-Structure.10368587.html
                {
                    var rx = new System.Text.RegularExpressions.Regex(@"\.\d+\.html");
                    var match = rx.Match(url);
                    if (match.Groups.Count == 1)
                    {
                        var rawId = match.Groups[0].Value;
                        if (rawId.Length == 0)
                        {
                            throw new ArgumentException("Incorrect URL");
                        }
                        rawId = rawId.Substring(1);
                        rawId = rawId.Substring(0, rawId.IndexOf('.'));
                        //ids.Add(string.Format("csids[{0}]=", ids.Count) + rawId);
                        ids.Add(int.Parse(rawId));
                        fileId = rawId;
                    }
                }
            }

            return ids;
        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
