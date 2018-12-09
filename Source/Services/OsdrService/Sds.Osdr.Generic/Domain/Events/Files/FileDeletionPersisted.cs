using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public interface FileDeletionPersisted
    {
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
