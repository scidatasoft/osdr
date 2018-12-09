using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public interface StatusPersisted
    {
        Guid Id { get; }
        RecordStatus Status { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
