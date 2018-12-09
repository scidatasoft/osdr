using MassTransit;
using System;

namespace Sds.Osdr.Reactions.Sagas.Events
{
    public interface ReactionProcessingFailed : CorrelatedBy<Guid>
    {
        Guid FileId { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
