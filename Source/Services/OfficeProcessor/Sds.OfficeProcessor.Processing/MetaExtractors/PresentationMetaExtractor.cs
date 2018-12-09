using Aspose.Slides;
using Newtonsoft.Json;
using Sds.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sds.OfficeProcessor.Processing.MetaExtractors
{
    public class PresentationMetaExtractor : IMetaExtractor
    {
        public IList<Property> GetMeta(Stream stream)
        {
            var presentation = new Presentation(stream);

            var props = presentation.DocumentProperties;

            var meta = new List<Property>();

            Type type = props.GetType();

            var fields = type.GetProperties(BindingFlags.Public
                | BindingFlags.Instance
                | BindingFlags.DeclaredOnly);

            foreach (var f in fields)
            {
                try
                {
                    if (!string.IsNullOrEmpty(f.GetValue(props) as string))
                    {
                        meta.Add(new Property(f.Name, JsonConvert.SerializeObject(f.GetValue(props))));
                    }
                }
                catch { }
            }

            return meta;
        }
    }
}
