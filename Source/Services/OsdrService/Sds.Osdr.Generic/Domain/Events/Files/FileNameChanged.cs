using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public class FileNameChanged : IUserEvent
    {
        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public Guid UserId { get; }
        public string OldName { get; set; }
        public string NewName { get; set; }

        public FileNameChanged(Guid id, Guid userId, string oldName, string newName)
        {
            Id = id;
            UserId = userId;
            OldName = oldName;
            NewName = newName;
        }
    }
}
