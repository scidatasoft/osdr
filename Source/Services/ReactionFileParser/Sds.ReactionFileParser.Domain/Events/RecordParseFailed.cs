using MassTransit;
using System;

namespace Sds.ReactionFileParser.Domain.Events
{
    public interface RecordParseFailed : CorrelatedBy<Guid>
    {
        string Message { get; }
        Guid FileId { get; }
        long Index { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
