using Sds.CqrsLite.Events;
using Sds.Osdr.Generic.Domain.ValueObjects;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public interface PermissionChangedPersisted : IUserEvent
    {
        Guid ParentId { get; }
        AccessPermissions AccessPermissions { get;}
    }
}
