using Sds.CqrsLite.Events;
using Sds.Osdr.Generic.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class PermissionsChanged : IUserEvent
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }

        public AccessPermissions AccessPermissions { get; private set; }

        public PermissionsChanged(Guid id, Guid userId, AccessPermissions accessPermissions)
        {
            Id = id;
            UserId = userId;
            AccessPermissions = accessPermissions;
        }
    }
}
