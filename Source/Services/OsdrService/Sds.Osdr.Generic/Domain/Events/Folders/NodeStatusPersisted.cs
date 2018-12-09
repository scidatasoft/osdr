using System;

namespace Sds.Osdr.Generic.Domain.Events.Folders
{
    public interface NodeStatusPersisted
    {
        Guid Id { get; }
        Guid ParentId { get; }
        FolderStatus Status { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
        int Version { get; }
    }
}
