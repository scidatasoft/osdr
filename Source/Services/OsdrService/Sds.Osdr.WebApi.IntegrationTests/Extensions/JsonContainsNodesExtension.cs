using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Sds.Osdr.WebApi.IntegrationTests.Extensions
{
    public static class JsonContainsNodesExtension
    {
        public static int ContainsNodes(this JToken nodes, IList<Guid> internalIds)
        {
            var countValid = nodes.Count(node => internalIds.Contains(node["id"].ToObject<Guid>()));
            return countValid;
        }
    }
}