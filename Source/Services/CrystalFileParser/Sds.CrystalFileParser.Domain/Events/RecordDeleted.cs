using MassTransit;
using System;

namespace Sds.CrystalFileParser.Domain.Events
{
    public interface RecordDeleted : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid FileId { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
