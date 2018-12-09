using System;
using MassTransit;

namespace Sds.Osdr.Generic.Domain.Events.Files
{
    public interface NodeStatusPersisted
    {
        Guid Id { get; }
        FileStatus Status { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
        Guid ParentId { get; }
        int Version { get; }
    }
}
