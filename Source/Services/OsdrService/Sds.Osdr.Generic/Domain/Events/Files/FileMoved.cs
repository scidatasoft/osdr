using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class FileMoved : IUserEvent
    {
        public Guid Id { get ; set ; }
        public int Version { get ; set ; }
        public DateTimeOffset TimeStamp { get ; set; } = DateTimeOffset.UtcNow;
        public Guid UserId { get; set; }
        public Guid? OldParentId { get; set; }
        public Guid? NewParentId { get; set; }

        public FileMoved(Guid id, Guid userId, Guid? oldParentId, Guid? newParentId)
        {
            Id = id;
            UserId = userId;
            OldParentId = oldParentId;
            NewParentId = newParentId;
        }
    }
}
