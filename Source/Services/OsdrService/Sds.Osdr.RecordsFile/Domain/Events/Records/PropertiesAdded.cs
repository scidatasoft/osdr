using Sds.CqrsLite.Events;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class PropertiesAdded : IUserEvent
    {
        public readonly IEnumerable<Property> Properties;

        public PropertiesAdded(Guid id, Guid userId, IEnumerable<Property> properties)
        {
            Id = id;
            UserId = userId;
            Properties = properties;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
