using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public interface NodeStatusPersisted
    {
        Guid Id { get; }
        RecordStatus Status { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
