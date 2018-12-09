using Sds.CqrsLite.Events;
using Sds.Domain;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class PropertyAdded : IUserEvent
    {
        public readonly Property Property;

        public PropertyAdded(Guid id, Guid userId, Property property)
        {
            Id = id;
            UserId = userId;
            Property = property;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
