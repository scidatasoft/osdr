using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Folders
{
    public class FolderParentPersisted : IUserEvent
    {
        public readonly Guid? ParentId;

        public FolderParentPersisted(Guid id, Guid userId, Guid? parentId)
        {
            Id = id;
            UserId = userId;
            ParentId = parentId;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
