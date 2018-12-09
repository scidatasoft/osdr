using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Files
{
    public interface TotalRecordsPersisted
    {
        Guid Id { get; }
        long TotalRecords { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
