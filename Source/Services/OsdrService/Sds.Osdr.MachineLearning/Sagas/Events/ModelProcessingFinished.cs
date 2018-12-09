using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Sagas.Events
{
    public interface ModelTrainingFinished : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        ModelStatus Status { get; }
        ModelInfo ModelInfo { get; }
        Guid ParentId { get; }
        Guid UserId { get; }
    }
}
