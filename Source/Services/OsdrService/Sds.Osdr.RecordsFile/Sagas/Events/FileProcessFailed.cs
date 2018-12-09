using MassTransit;
using System;

namespace Sds.Osdr.RecordsFile.Sagas.Events
{
    public interface FileProcessFailed : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        long ProcessedRecords { get; }
        long FailedRecords { get; }
        string Message { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
