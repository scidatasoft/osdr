using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using WikiTools.Access;
using static WikiTools.Access.WikiConfigSection;
using System.Threading.Tasks;
using System.Threading;

namespace Sds.WebImporter.ChemicalProcessing.CommandHandlers
{
    public class Wikipedia
    {
        public string Content { get; }

        public Dictionary<string, object> Meta { get; }

        private static WikiConfigSection Config
        {
            get
            {
                return ConfigurationManager.GetSection("wiki") as WikiConfigSection;
            }
        }

        public Wikipedia(IEnumerable<string> urls)
        {
            var templates = new List<string>();
            foreach (TemplateElement templateName in Config.TemplateNames)
            {
                templates.Add(templateName.Name);
            }


            JObject mergedJson = null;

            foreach (var url in urls)
            {
                var pageName = ParseUrl(url).Value;
                Wiki sourceWiki = new Wiki("https://" + this.ParseUrl(url).Key + ".wikipedia.org/w");
                var export = new Export(sourceWiki);
                Page page = sourceWiki.GetPage(HttpUtility.UrlDecode(pageName));
                
                var json = export.ExportPages(new List<Page>() { page }, 1, templates);

                if (mergedJson == null)
                {
                    mergedJson = JObject.Parse(json);
                }
                else
                {
                    mergedJson.Merge(JObject.Parse(json));
                }
            };

            Content = mergedJson.ToString();
            var uri = new Uri(urls.First());
            var splitedHost = uri.Host.Split('.').ToArray();
            var host = (splitedHost.Count() > 2 ? $"{splitedHost[splitedHost.Count() - 2]}.{splitedHost[splitedHost.Count() - 1]}" : uri.Host);


            Meta = new Dictionary<string, object>() {
                { "ImportedFrom", host },
                { "Url", uri.AbsoluteUri },
                { "Source Content Type", "application/json" },
            };

            //return fileStorage.AddFile(path, GetFileName(), Encoding.UTF8.GetBytes(outputJson), meta);
        }

        private KeyValuePair<string, string> ParseUrl(string url)
        {
            var ids = new List<string>();
            Regex regexp = new Regex(@"(?<wiki>https?:\/\/(?<country>.{2}).wikipedia.org\/wiki\/)(?<page>(.*))");
            Match match = regexp.Match(url);
            var page = match.Groups["page"].Value;
            var country = match.Groups["country"].Value;
            KeyValuePair<string, string> pageCountry = new KeyValuePair<string, string>(country, page);
            return pageCountry;
        }
    }
}
