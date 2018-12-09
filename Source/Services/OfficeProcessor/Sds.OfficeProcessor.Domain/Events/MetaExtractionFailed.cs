using MassTransit;
using System;

namespace Sds.OfficeProcessor.Domain.Events
{
    public interface MetaExtractionFailed : CorrelatedBy<Guid>
    {
        string Message { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
