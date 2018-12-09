using CQRSlite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Nodes
{
    public interface MovedFilePersisted : IEvent
    {
        Guid UserId { get; set; }
        Guid OldParentId { get; set; }
        Guid NewParentId { get; set; }
        string TargetFolderName { get; set; }
    }
}
