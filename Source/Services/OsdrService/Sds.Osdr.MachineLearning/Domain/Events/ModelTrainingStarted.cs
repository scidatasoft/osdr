using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ModelTrainingStarted : CorrelatedBy<Guid>
    {
        Guid Id { get; set; }
        Guid UserId { get; }
        string ModelName { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
