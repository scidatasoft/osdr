using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    [Serializable]
    public class FeatureVectorsCalculationFailed : CorrelatedBy<Guid>
    {
        public string Message { get; set; }

        public Guid CorrelationId { get; set; }
    }
}
