using Sds.Osdr.MachineLearning.Domain;
using System.Collections.Generic;

namespace Sds.Osdr.WebApi.Requests
{
    public class RunFeatureVectorCalculation
    {
        public IEnumerable<Fingerprint> Fingerprints { set; get; }
    }
}
