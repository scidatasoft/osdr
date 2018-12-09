using Sds.ChemicalExport.Domain.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sds.ChemicalExport.Domain.Interfaces
{
    public interface IChemicalExport
    {
        Task Export(IEnumerator<Record> records, Stream otputStream, IEnumerable<string> properties, IDictionary<string, string> map);
    }
}
