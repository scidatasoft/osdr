using Aspose.Words;
using System.Collections.Generic;
using System.IO;
using Aspose.Words.Properties;
using Sds.Domain;
using Newtonsoft.Json;

namespace Sds.OfficeProcessor.Processing.MetaExtractors
{
    public class DocMetaExtractor : IMetaExtractor
    {
        public IList<Property> GetMeta(Stream stream)
        {
            var doc = new Document(stream);

            var props = doc.BuiltInDocumentProperties;

            var meta = new List<Property>();// Dictionary<string, object>();

            foreach (DocumentProperty p in props)
            {
                meta.Add(new Property(p.Name, JsonConvert.SerializeObject(p.Value)));
            }
              
            return meta;
        }
    }
}
