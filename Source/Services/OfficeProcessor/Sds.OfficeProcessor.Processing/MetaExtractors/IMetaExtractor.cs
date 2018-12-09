using Sds.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.OfficeProcessor.Processing.MetaExtractors
{
    public interface IMetaExtractor
    {
        IList<Property> GetMeta(Stream stream);
    }
}
