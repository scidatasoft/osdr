using MassTransit;
using System;

namespace Sds.CrystalFileParser.Domain.Events
{
    public interface FileDeleted : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
