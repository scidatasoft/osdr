using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.WebImporter.ChemicalProcessing.CommandHandlers
{
    public static class CategoryUri
    {
        public const string RECORD_INTRINSIC = "https://osdr.your-company.com/record/intrinsic";
        public const string RECORD_USER = "https://osdr.your-company.com/record/user";
        public const string RECORD_IMPORTED_FILE = "https://osdr.your-company.com/record/imported/file";

        public const string RECORD_WIKI_INTRINSIC = "https://en.wikipedia.org/intinsic";

        public const string RECORD_CHEMSPIDER_INTRINSIC = "https://www.chemspider.com/intrinsic";
        public const string RECORD_CHEMSPIDER_EXPERIMENTAL = "https://www.chemspider.com/experimental";
        public const string RECORD_CHEMSPIDER_PREDICTED = "https://www.chemspider.com/predicted";

        public const string RECORD_TRIPOD_INTRISTIC = "https://tripod.nih.gov/intristic";

        public const string FILE_INTRINSIC = "https://osdr.your-company.com/file/intrinsic";
        public const string FILE_USER = "https://osdr.your-company.com/file/user";
        public const string FILE_IMPORTED = "https://osdr.your-company.com/file/imported";

        public const string FOLDER_INTRINSIC = "https://osdr.your-company.com/folder/intrinsic";

    }

    public class Category
    {
        public string URI { get; set; }
        public string Title { get; set; }
        public string Source { get; set; }
        public bool Editable { get; set; }
        public bool Extendable { get; set; }
        public IEnumerable<FileParser.PropertyValue> Properties { get; set; }

    }
    //public class CategoryDefinition
    //{
    //    public string URI { get; set; }
    //    public string Title { get; set; }
    //    public string Source { get; set; }
    //    public bool Editable { get; set; }
    //    public bool Extendable { get; set; }
    //    public IEnumerable<PropertyDefinition> Properties { get; set; }
    //}
}
