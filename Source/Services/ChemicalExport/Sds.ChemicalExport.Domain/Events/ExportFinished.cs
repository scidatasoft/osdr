using System;

namespace Sds.ChemicalExport.Domain.Events
{
    public interface ExportFinished
    {
        /// <summary>
        /// Export Id
        /// </summary>
        Guid Id { get; }
        /// <summary>
        /// Blob Id of sourse file that should be exported
        /// </summary>
        Guid SourceBlobId { get; }
        /// <summary>
        /// Exported file's bucket name
        /// </summary>
        string ExportBucket { get; }
        /// <summary>
        /// Blob Id that has been exported
        /// </summary>
        Guid ExportBlobId { get; }
        /// <summary>
        /// Export format (SDF, CSV, SPL)
        /// </summary>
        string Format { get; }

        string Filename { get; }

        Guid UserId { get; }

        DateTimeOffset TimeStamp { get; }
    }
}
