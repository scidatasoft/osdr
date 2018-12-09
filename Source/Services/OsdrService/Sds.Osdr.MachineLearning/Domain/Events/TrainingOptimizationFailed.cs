using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface TrainingOptimizationFailed : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        string Message { get; }
    }
}
