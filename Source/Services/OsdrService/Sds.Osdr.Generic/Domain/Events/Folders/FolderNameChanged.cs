using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Folders
{
    public class FolderNameChanged : IUserEvent
    {
        public string OldName { get; set; }
        public string NewName { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public Guid UserId { get; set; }
        public Guid SessionId { get; }
        public FolderNameChanged(Guid id, Guid userId, string oldName, string newName)
        {
            Id = id;
            UserId = userId;
            OldName = oldName;
            NewName = newName;
        }
    }
}
