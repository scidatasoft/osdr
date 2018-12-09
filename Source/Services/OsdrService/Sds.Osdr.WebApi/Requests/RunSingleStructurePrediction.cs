using System;
using System.Collections.Generic;

namespace Sds.Osdr.WebApi.Requests
{
    public class RunSingleStructurePrediction
    {
        public string Structure { set;  get; }
        public string Format { set; get; } // SMILES or MOL
        public string PropertyName { get; set; }
        public IEnumerable<Guid> ModelIds { set; get; }
    }
}
