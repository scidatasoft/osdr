using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.ChemicalExport.Domain.Events
{
    public interface ExportFailed
    {
        /// <summary>
        /// Export Id
        /// </summary>
        Guid Id { get; }
        /// <summary>
        /// File Id that should be exported
        /// </summary>
        Guid FileId { get; }
        /// <summary>
        /// File's bucket
        /// </summary>
        string Bucket { get; }
        /// <summary>
        /// Export format
        /// </summary>
        string Format { get; }
        /// <summary>
        /// Error message
        /// </summary>
        string Message { get; }

        DateTimeOffset TimeStamp { get; }

    }
}
