using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class ModelNameUpdated : IUserEvent
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public Guid UserId { get; set; }
        public string Name { get; set; }

        public ModelNameUpdated(Guid id, Guid userId, string name)
        {
            Id = id;
            UserId = userId;
            Name = name;
        }
    }
}
