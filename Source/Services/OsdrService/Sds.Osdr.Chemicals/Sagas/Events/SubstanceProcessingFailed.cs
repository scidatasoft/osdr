using MassTransit;
using System;

namespace Sds.Osdr.Chemicals.Sagas.Events
{
    public interface SubstanceProcessingFailed : CorrelatedBy<Guid>
    {
        Guid FileId { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
