using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Storage.Blob.Core
{
    public interface IBlob : IDisposable
    {
        /// <summary>
        /// Provide information about stored file
        /// </summary>
        IBlobInfo Info { get; }

        /// <summary>
        /// Returns Stream with the file's data
        /// </summary>
        /// <returns></returns>
        Stream GetContentAsStream();
    }
}
