using System.Collections.Generic;

namespace Sds.ChemicalExport.Domain.Models
{
    public enum PropertyType
    {
        String = 0,
        Int = 1,
        Bool = 2,
        Email = 3,
        Url = 4,
        Json = 5,
        Double = 6,
        Select = 7,
        Date = 8,
        Time = 9,
        Datetime = 10,
        Textarea = 11
    }

    public class PropertyValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public PropertyType Type { get; set; }
    }

    public class Record
    {
        public long Index { get; set; }
        public string Mol { get; set; }
        public string Smiles { get; set; }
        public IEnumerable<PropertyValue> Properties { get; set; }
    }

    public static class MimeType
    {
        private static IDictionary<string, string> mimes = new Dictionary<string, string>()
        {
            {".sdf", "chemical/x-mdl-sdfile"},
            {".mol", "chemical/x-mdl-molfile"},
            {".jdx", "chemical/x-jcamp-dx"},
            {".dx", "chemical/x-jcamp-dx"},
            {".rdx", "chemical/x-mdl-rxnfile"},
            {".rdf", "chemical/x-mdl-rdfile"},
            {".cif", "chemical/x-cif"},
            {".cdx", "chemical/x-cdx"}
        };

        public static string GetFileMIME(string extention)
        {
            if (mimes.ContainsKey(extention))
                return mimes[extention];

            return "application/octet-stream";
        }
    } 
    
}
