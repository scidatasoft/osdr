using MassTransit;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface CreateModel : CorrelatedBy<Guid>
    {
        Guid ParentId { get; }
        string Method { get; }
        Dataset Dataset { get; }
        Guid Id { get; }
        Guid UserId { get; }
        int KFold { get; }
        decimal TestDatasetSize { get; }
        decimal SubSampleSize { get; }
        string ClassName { get; }
        string Scaler { get; }
        IEnumerable<Fingerprint> Fingerprints { get; }
    }
}
