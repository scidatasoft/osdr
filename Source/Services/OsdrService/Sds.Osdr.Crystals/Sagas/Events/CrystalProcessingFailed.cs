using MassTransit;
using System;

namespace Sds.Osdr.Crystals.Sagas.Events
{
    public interface CrystalProcessingFailed : CorrelatedBy<Guid>
    {
        Guid FileId { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
