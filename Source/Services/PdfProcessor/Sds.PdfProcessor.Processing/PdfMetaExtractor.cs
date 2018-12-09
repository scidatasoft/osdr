using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sds.PdfProcessor.Processing
{
    public class PdfMetaExtractor
    {
        public Dictionary<string, object> Meta { get; }

        public PdfMetaExtractor(PdfDocument document)
        {
            Meta = new Dictionary<string, object>();

            var props = document.Info;

            Type type = props.GetType();

            foreach (var f in type.GetProperties(BindingFlags.Public
                | BindingFlags.Instance
                | BindingFlags.DeclaredOnly))
            {
                if(f.GetValue(props) != null && f.GetValue(props) != String.Empty)
                Meta.Add(f.Name, f.GetValue(props));
            }
        }
    }
}
