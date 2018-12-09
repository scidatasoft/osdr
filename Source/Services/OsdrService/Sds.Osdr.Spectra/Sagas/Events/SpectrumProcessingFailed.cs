using MassTransit;
using System;

namespace Sds.Osdr.Spectra.Sagas.Events
{
    public interface SpectrumProcessingFailed : CorrelatedBy<Guid>
    {
        Guid FileId { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
