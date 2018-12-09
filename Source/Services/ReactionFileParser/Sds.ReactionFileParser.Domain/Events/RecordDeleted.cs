using MassTransit;
using System;

namespace Sds.ReactionFileParser.Domain.Events
{
    public interface RecordDeleted : CorrelatedBy<Guid>
    {
        Guid FileId { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
