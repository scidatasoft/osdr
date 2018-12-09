using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ModelStatusPersisted
    {
        Guid Id { get; }
        ModelStatus Status { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
