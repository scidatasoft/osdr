using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class ModelDeleted : IUserEvent
    {
        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public Guid UserId { get; set; }
        public bool Force { get; set; }

        public ModelDeleted(Guid id, Guid userId, bool force)
        {
            Id = id;
            UserId = userId;
            Force = force;
        }
    }
}
