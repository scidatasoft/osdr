using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Sds.WebImporter.ChemicalProcessing.CommandHandlers
{
    public class Tripod
    {
        public string Sourse { get; }

        public Dictionary<string, object> Meta { get; }

        public Tripod(IEnumerable<string> urls)
        {
            Sourse = new WebClient().DownloadString(urls.First());
            var uri = new Uri(urls.First());

            Meta = new Dictionary<string, object>() {
                { "ImportedFrom", uri.Host },
                { "Url", uri.AbsoluteUri },
                { "Source Content Type", "application/json" },
            };

        }

        public string GetFileName()
        {
            return "Imported from Tripod on " + DateTime.Now.ToString("MMddyyyy.HHmmss") + ".json";
        }
    }
}
