using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface CalculateFeatureVectors : CorrelatedBy<Guid>
    {
        IEnumerable<Fingerprint> Fingerprints { get; }
        string FileType { get; }
    }
}
