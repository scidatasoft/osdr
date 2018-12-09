using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.ChemicalExport.Domain.Commands
{
    public interface ExportFile : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Export Id
        /// </summary>
        Guid Id { get; }
        /// <summary>
        /// File's bucket name
        /// </summary>
        string Bucket { get; }
        /// <summary>
        /// Blob Id of file that should be exported
        /// </summary>
        Guid BlobId { get; }
        /// <summary>
        /// Export format (SDF, CSV, SPL)
        /// </summary>
        string Format { get; }
        /// <summary>
        /// List of properties that should be included to the export
        /// </summary>
        /// <example>[Properties.Fields.InChI, Properties.Fileds.SMILES, Properties.ChemicalProperties.MOLECULAR_FORMULA]</example>
        IEnumerable<string> Properties { get; }
        /// <summary>
        /// </summary>
        /// <example>{{Properties.Fields.INCHI, Properties.Fields.InChI}}</example>
        IDictionary<string, string> Map { get; }

        Guid UserId { get; }
    }
}
