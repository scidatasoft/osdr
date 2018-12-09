using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public interface NodeRecordPersisted
    {
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
