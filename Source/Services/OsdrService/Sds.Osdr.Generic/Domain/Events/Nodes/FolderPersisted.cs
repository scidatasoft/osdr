using CQRSlite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Nodes
{
    public interface FolderPersisted : IEvent
    {
        Guid UserId { get; set; }
        Guid ParentId { get; set; }
        string Name { get; set; }
    }
}
