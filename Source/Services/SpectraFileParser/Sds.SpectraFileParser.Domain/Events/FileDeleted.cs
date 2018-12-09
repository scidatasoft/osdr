using MassTransit;
using System;

namespace Sds.SpectraFileParser.Domain.Events
{
    public interface FileDeleted : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
