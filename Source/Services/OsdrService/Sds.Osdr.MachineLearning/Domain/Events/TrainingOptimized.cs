using MassTransit;
using System;
using System.Collections.Generic;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface TrainingOptimized : CorrelatedBy<Guid>
    {
        decimal SubSampleSize { get; }
        decimal TestDataSize { get; }
        string Scaler { get; }
        int KFold { get; }
        IEnumerable<IDictionary<string, object>> Fingerprints { get; }
        Guid Id { get; }
        Guid UserId { get; }
        HyperParametersOptimization HyperParameters { get; }
    }
}
