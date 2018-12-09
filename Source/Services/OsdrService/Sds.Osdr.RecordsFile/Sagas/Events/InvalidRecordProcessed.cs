using MassTransit;
using Sds.Osdr.RecordsFile.Domain;
using System;

namespace Sds.Osdr.RecordsFile.Sagas.Events
{
    public interface InvalidRecordProcessed : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid FileId { get; }
        long Index { get; }
        RecordType Type { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
