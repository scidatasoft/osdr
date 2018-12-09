using Sds.ChemicalExport.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sds.ChemicalExport.Domain.Models;
using System.IO;

namespace Sds.ChemicalExport.Processing.Workers
{
    public class SplExport : IChemicalExport
    {
        public Task Export(IEnumerator<Record> records, Stream otputStream, IEnumerable<string> properties, IDictionary<string, string> map)
        {
            throw new NotImplementedException();
        }
    }
}
