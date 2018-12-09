using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Storage.Blob.Core
{
    public interface IBlobInfo
    {
        //
        // Summary:
        //     Gets the type of the content.
        string ContentType { get; }
        //
        // Summary:
        //     Gets the filename.
        string FileName { get; }
        //
        // Summary:
        //     Gets the identifier.
        Guid Id { get; }
        //
        // Summary:
        //     Gets the length.
        long Length { get; }
        //
        // Summary:
        //     Gets the metadata.
        IDictionary<string, object> Metadata { get; }
        //
        // Summary:
        //     Gets the MD5 checksum.
        string MD5 { get; }
        //
        // Summary:
        //     Gets the upload date time.
        DateTime UploadDateTime { get; }
    }
}
