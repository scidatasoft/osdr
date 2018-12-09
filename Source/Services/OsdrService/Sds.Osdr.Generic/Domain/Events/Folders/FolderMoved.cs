using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Folders
{
    public class FolderMoved : IUserEvent, ISessionEvent
    {
        public Guid Id { get ; set ; }
        public int Version { get ; set ; }
        public DateTimeOffset TimeStamp { get ; set; } = DateTimeOffset.UtcNow;
        public Guid SessionId { get;  }
        public Guid UserId { get; set; }

        public readonly Guid? OldParentId;
        public readonly Guid? NewParentId;

        public FolderMoved(Guid id, Guid userId, Guid? oldParentId, Guid? newParentId, Guid sessionId)
        {
            Id = id;
            UserId = userId;
            OldParentId = oldParentId;
            NewParentId = newParentId;
            SessionId = sessionId;
        }
    }
}
