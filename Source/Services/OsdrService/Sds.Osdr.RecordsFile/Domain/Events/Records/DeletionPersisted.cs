using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public interface DeletionPersisted
    {
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
