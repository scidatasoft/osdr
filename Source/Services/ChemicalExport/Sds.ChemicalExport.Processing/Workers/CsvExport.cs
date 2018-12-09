using CsvHelper;
using Sds.ChemicalExport.Domain.Interfaces;
using Sds.ChemicalExport.Domain.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.ChemicalExport.Processing.Workers
{
    public class CsvExport : IChemicalExport
    {
        //public async Task Export(IRecordsProvider recordsEnumerator, Stream otputStream, IEnumerable<string> properties, IDictionary<string, string> map)
        public async Task Export(IEnumerator<Record> recordsEnumerator, Stream otputStream, IEnumerable<string> properties, IDictionary<string, string> map)
        {
            TextWriter sw = new StreamWriter(otputStream);

            var csv = new CsvWriter(sw);

            var mapProperties = new Dictionary<string, Dictionary<long, object>>();
            var numOfRecords = 0;
            var enumerator = recordsEnumerator;
            IList<string> colNames = new List<string>();
            colNames.Add("Properties.ChemicalProperties.SMILES");
            mapProperties.Add("Properties.ChemicalProperties.SMILES", new Dictionary<long, object>());
            foreach (var property in properties)
            {
                //if (map.ContainsKey(property))
                //{
                //    mapProperties.Add(map[property], new Dictionary<long, object>());
                //}
                //else
                //{
                mapProperties.Add(property, new Dictionary<long, object>());

                if (map.ContainsKey(property))
                {
                    if (!colNames.Contains(property))
                    {
                        colNames.Add(map[property]);
                    }                        
                }
                else
                {
                    colNames.Add(property);
                }
                //}
            }

            foreach (var colName in colNames)
            {
                csv.WriteField(colName);
            }
            csv.NextRecord();

            while (recordsEnumerator.MoveNext())
            {
                var record = recordsEnumerator.Current;
                numOfRecords++;

                var smilesList = record.Properties.Where(p => p.Name.Contains("SMILES")).Select(p => p.Value).ToList();
                var chemicalSmiles = "";
                if (smilesList.Count > 1)
                {
                    chemicalSmiles = record.Properties.Where(p => p.Name.Contains("ChemicalProperties.SMILES")).Select(p => p.Value).First();
                }
                else
                {
                    chemicalSmiles = smilesList.Single();
                }

                mapProperties["Properties.ChemicalProperties.SMILES"][record.Index] = chemicalSmiles;

                foreach (var property in record.Properties)
                {
                    if (properties.Contains(property.Name))
                    {
                        //if (map.ContainsKey(property.Name))
                        //{
                        //    mapProperties[map[property.Name]][record.Index] = property.Value;
                        //    colNames.Add(map[property.Name]);
                        //}
                        //else
                        //{
                        mapProperties[property.Name][record.Index] = property.Value;
                        //}
                    }
                }
            }
            //colNames = colNames.Distinct().ToList();

            for (int i = 0; i < numOfRecords; i++)
            {
                foreach (var property in properties)
                {
                    if (mapProperties[property].ContainsKey(i))
                    {
                        csv.WriteField(mapProperties[property][i]);
                    }
                    else
                    {
                        csv.WriteField("");
                    }
                }

                csv.NextRecord();
            }
            //for (int i = 0; i < mapProperties.Count; i++)
            //{
            //foreach(var cell in mapProperties.ElementAt(i).Value)
            //{
            //    if (mapProperties[colName][i] != null)
            //    {
            //        csv.WriteField(mapProperties[colName][i]);
            //    }
            //    else
            //    {
            //        csv.WriteField("");
            //    }
            //}
            //foreach (var colName in colNames)
            //{
            //    if(mapProperties[colName][i] != null)
            //    {
            //        csv.WriteField(mapProperties[colName][i]);
            //    }
            //    else
            //    {
            //        csv.WriteField("");
            //    }
            //}
            //csv.NextRecord();
            //}

            //csv.WriteField($"Index");

            //foreach(var property in filteredProperties)
            //{
            //    if(map.ContainsKey(property))
            //    {
            //        csv.WriteField(map[property]);
            //    }
            //    else
            //    {
            //        csv.WriteField(property);
            //    }
            //}
            //csv.NextRecord();
            //Dictionary<string, int> propertiesIndex = new Dictionary<string, int>();


            //while (recordsEnumerator.MoveNext())
            //{
            //    var record = recordsEnumerator.Current;
            //    csv.WriteField($"Index: {record.Index}");

            //    foreach (var property in record.Properties)
            //    {
            //        if(filteredProperties.Contains(property.Name))
            //        {
            //            string p = "";
            //            if (map.ContainsKey(property.Name))
            //            {
            //                p = map[property.Name] + " : " + property.Value;
            //            }
            //            else
            //            {
            //                p = property.Name + " : " + property.Value;
            //            }
            //            csv.WriteField(p);
            //        }
            //    }
            //    csv.NextRecord();
            //}
            sw.Flush();
        }
    }
}
