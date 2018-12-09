using Aspose.Cells;
using Newtonsoft.Json;
using Sds.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Sds.OfficeProcessor.Processing.MetaExtractors
{
    public class ExcelMetaExtractor : IMetaExtractor
    {
        public IList<Property> GetMeta(Stream stream)
        {
            var workbook = new Workbook(stream);

            var props = workbook.BuiltInDocumentProperties;

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
