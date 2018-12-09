using CQRSlite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Nodes
{
    public interface MovedFolderPersisted : IEvent
    {
        Guid UserId { get; set; }
        Guid OldParentId { get; set; }
        Guid NewParentId { get; set; }
    }
}
