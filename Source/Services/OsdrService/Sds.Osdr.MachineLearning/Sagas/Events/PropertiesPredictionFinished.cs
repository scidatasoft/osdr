using MassTransit;
using Sds.Osdr.Generic.Domain;
using System;

namespace Sds.Osdr.MachineLearning.Sagas.Events
{
    public interface PropertiesPredictionFinished : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
        string Message { get; }
    }
}
