using CQRSlite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Nodes
{
    public interface DeletedFilePersisted : IEvent
    {
        Guid UserId { get; set; }
        Guid ParentId { get; set; }
    }
}
