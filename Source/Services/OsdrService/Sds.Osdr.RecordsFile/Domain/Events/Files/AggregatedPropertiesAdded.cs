using Sds.CqrsLite.Events;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Events.Files
{
    public class AggregatedPropertiesAdded : IUserEvent
    {
        public IEnumerable<string> Properties { get; set; }
        public Guid UserId { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public Guid Id { get; set; }

        public AggregatedPropertiesAdded(Guid id, Guid userId, IEnumerable<string> properties)
        {
            Id = id;
            UserId = userId;
            Properties = properties;
        }
    }
}
