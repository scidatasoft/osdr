using System;

namespace Sds.Osdr.Generic.Domain.Events
{
    public interface UnexpectedVersion
    {
        Guid Id { get; }
        Guid UserId { get; }
        int Version { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
