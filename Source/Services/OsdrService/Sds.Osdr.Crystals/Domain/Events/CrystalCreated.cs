using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Crystals.Domain.Events
{
    public class CrystalCreated : IUserEvent
    {
        public CrystalCreated(Guid id, Guid userId)
        {
            Id = id;
            UserId = userId;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
