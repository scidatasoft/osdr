using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    [Serializable]
    public class FeatureVectorsCalculated : CorrelatedBy<Guid>
    {
        public int Structures { get; set; }
        public int Columns { get; set; }
        public int Failed { get; set; }
        public Guid CorrelationId { get; set; }
    }
}
