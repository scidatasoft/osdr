using Sds.ChemicalExport.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sds.ChemicalExport.Domain.Models;
using System.IO;

namespace Sds.ChemicalExport.Processing.Workers
{
    public class SdfExport : IChemicalExport
    {
        private List<string> Properties;
        private Dictionary<string, string> Map;

        public async Task Export(IEnumerator<Record> recordsEnumerator, Stream otputStream, IEnumerable<string> properties, IDictionary<string, string> map)
        {
                      
            var sw = new StreamWriter(otputStream);
            
            Properties = properties.ToList();
            Map = map.ToDictionary(m => m.Key, m => m.Value);

            StringBuilder sb = new StringBuilder();

            while (recordsEnumerator.MoveNext())
            {
                var record = recordsEnumerator.Current;

                sw.WriteLine(RecordToSdf(record));
            }
            sw.Flush();
            
        }

        private string RecordToSdf(Record record)
        {
            StringBuilder sb = new StringBuilder();

            if (record.Mol == null)
            {
                return "";
            }
            sb.AppendLine(record.Mol);
            //sb.AppendLine("M END");
            foreach (var property in record.Properties)
            {
                if(Properties.Contains(property.Name))
                {
                    if (Map.ContainsKey(property.Name))
                    {
                        sb.AppendLine($"> <{Map[property.Name]}>");
                    }
                    else
                    {
                        sb.AppendLine($"> <{property.Name}>");
                    }
                    sb.AppendLine(property.Value);
                    sb.AppendLine();
                }
            }
            sb.AppendLine("$$$$");
            return sb.ToString();
        }
    }
}
