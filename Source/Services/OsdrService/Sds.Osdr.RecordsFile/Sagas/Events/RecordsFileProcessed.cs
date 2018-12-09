using MassTransit;
using System;

namespace Sds.Osdr.RecordsFile.Sagas.Events
{
    public interface RecordsFileProcessed : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid BlobId { get; }
        string Bucket { get; }
        long ProcessedRecords { get; }
        long FailedRecords { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
