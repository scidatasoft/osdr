using MassTransit;
using System;

namespace Sds.ReactionFileParser.Domain.Events
{
    public interface FileParseFailed : CorrelatedBy<Guid>
    {
        string Message { get; }
        long RecordsProcessed { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
