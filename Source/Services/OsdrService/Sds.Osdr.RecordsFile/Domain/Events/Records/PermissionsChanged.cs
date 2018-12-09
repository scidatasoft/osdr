using System;
using Sds.CqrsLite.Events;
using Sds.Osdr.Generic.Domain.ValueObjects;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class PermissionsChanged : IUserEvent
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }

        public  AccessPermissions AccessPermissions { get; private set; }

        public PermissionsChanged(Guid id, Guid userId, AccessPermissions accessPermissions)
        {
            Id = id;
            UserId = userId;
            AccessPermissions = accessPermissions;
        }
    }
}