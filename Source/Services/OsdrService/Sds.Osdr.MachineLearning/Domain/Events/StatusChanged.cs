using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class StatusChanged : IUserEvent
    {
        public ModelStatus Status { get; set; }

        public StatusChanged(Guid id, Guid userId, ModelStatus status)
        {
            Id = id;
            UserId = userId;
            Status = status;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
