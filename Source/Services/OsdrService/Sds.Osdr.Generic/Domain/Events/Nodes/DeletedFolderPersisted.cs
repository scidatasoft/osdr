using CQRSlite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Nodes
{
    public interface DeletedFolderPersisted : IEvent
    {
        Guid UserId { get; set; }
        Guid ParentId { get; set; }
    }
}
