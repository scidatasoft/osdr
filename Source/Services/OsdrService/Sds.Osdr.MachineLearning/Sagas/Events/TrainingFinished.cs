using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public enum TrainingStatus
    {
        Started,
        Training,
        Fimished,
        Failed
    }

    public interface TrainingFinished : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
        int NumberOfGenericFiles { get; }
        TrainingStatus Status { get; }
        string Message { get; }
    }
}

