using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.FileParser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sds.WebImporter.ChemicalProcessing.CommandHandlers
{
    public class WikipediaReader : IRecordReader
    {
        private readonly Stream stream;

        public WikipediaReader(Stream stream)
        {
            this.stream = stream;
        }

        public IEnumerable<string> Extensions()
        {
            return new[] { ".json" };
        }

        public IEnumerator<Record> GetEnumerator()
        {
            return new WikipediaEnumerable(this.stream);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class WikipediaEnumerable : IEnumerator<Record>
    {
        private int currentIndex = -1;
        private JObject records;
        private IList<string> keys;

        public WikipediaEnumerable(Stream stream)
        {
            records = JsonConvert.DeserializeObject<JObject>(new StreamReader(stream).ReadToEnd());
            keys = records.Properties().Select(p => p.Name).ToList();
        }

        private IEnumerable<PropertyValue> GetProperties()
        {
            var record = records[keys[currentIndex]];

            List<PropertyValue> properties = new List<PropertyValue>();
            foreach (JProperty property in record.Children())
            {
                var strValue = property.Value.ToString();
                properties.Add(new PropertyValue()
                {
                    Name = property.Name,
                    Value = strValue,
                    Type = strValue.ToPropertyType()
                });
            }

            return properties;
        }

        private Category GetCategory()
        {
            return new Category()
            {
                Editable = true,
                Properties = GetProperties(),
                Title = "Wikipedia",
                URI = CategoryUri.RECORD_WIKI_INTRINSIC,
                Source = "wikipedia.org",
            };
        }

        public string ChemIdToMol(int chemID)
        {
            JSONClient client = new JSONClient();
            var compounds = client.GetRecordsAsCompounds(new List<int>() { chemID });
            return compounds.First().Mol;
        }

        public string GetMol()
        {
            N2SResult res = new N2SResult();
            JSONClient client = new JSONClient();

            var mol = "";
            var currentValue = records[keys[this.currentIndex]];


            var chemID = currentValue["chemspiderid"];
            var inChIKey = currentValue["stdinchikey"];
            var name = keys[this.currentIndex];

            if (chemID != null)
            {
                mol = this.ChemIdToMol(Int32.Parse(chemID.ToString()));
            }
            else if (inChIKey != null)
            {
                res = client.ConvertTo(new ConvertOptions() { Direction = ConvertOptions.EDirection.InChiKey2ID, Text = inChIKey.ToString() });
                mol = ChemIdToMol(Int32.Parse(res.mol));
            }
            else
            {
                res = client.ConvertTo(new ConvertOptions() { Direction = ConvertOptions.EDirection.Name2Mol, Text = name });
                mol = res.mol;
            }

            return mol;
        }

        public Record Current
        {
            get
            {
                return new Record()
                {
                    Data = GetMol(),
                    Index = currentIndex,
                    Type = RecordType.Chemical,
                    Properties = GetProperties()
                };
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            this.currentIndex++;
            return currentIndex < this.keys.Count;
        }

        public void Reset()
        {
            this.currentIndex = -1;
        }
    }
}
