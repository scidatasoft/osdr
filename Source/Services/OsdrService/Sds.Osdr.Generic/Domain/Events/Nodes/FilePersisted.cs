using CQRSlite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Nodes
{
    public interface FilePersisted : IEvent
    {
        Guid UserId { get; set; }
        Guid ParentId { get; set; }
        string FileName { get; set; }
    }
}
