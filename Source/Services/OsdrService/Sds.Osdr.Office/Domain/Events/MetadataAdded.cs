using Sds.CqrsLite.Events;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Office.Domain.Events
{
    public class MetadataAdded : IUserEvent
    {
        public readonly IEnumerable<Property> Metadata;

        public MetadataAdded(Guid id, Guid userId, IEnumerable<Property> metadata)
        {
            Id = id;
            UserId = userId;
            Metadata = metadata;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
