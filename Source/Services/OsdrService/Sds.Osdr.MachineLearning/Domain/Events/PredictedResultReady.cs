using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface PredictedResultReady : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        dynamic Data { get; }
    }
}
