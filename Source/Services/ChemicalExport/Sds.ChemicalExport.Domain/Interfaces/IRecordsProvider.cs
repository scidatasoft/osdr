using Sds.ChemicalExport.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sds.ChemicalExport.Domain
{
    public interface IRecordsProvider
    {
        Task<IEnumerator<Record>> GetRecords(Guid blobId, string bucket
            //, int start = 0, int count = 100
            );
    }
}
