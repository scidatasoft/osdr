using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Requests
{
    public class Property
    {
        public string Name { get; set; }
        public string ExportName { get; set; }
    }

    public class ExportFileRequest
    {
        /// <summary>
        /// File Id that should be exported
        /// </summary>
        public Guid BlobId { get; set; }
        /// <summary>
        /// Export format (SDF, CSV, SPL)
        /// </summary>
        public string Format { get; set; }
        /// <summary>
        /// List of properties that should be included to the export
        /// </summary>
        /// <example>[Properties.Fields.InChI, Properties.Fileds.SMILES, Properties.ChemicalProperties.MOLECULAR_FORMULA]</example>
        public IEnumerable<Property> Properties { get; set; }
    }
}
