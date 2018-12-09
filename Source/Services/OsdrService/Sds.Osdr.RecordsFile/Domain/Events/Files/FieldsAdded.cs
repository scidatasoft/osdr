using Sds.CqrsLite.Events;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Events.Files
{
    public class FieldsAdded : IUserEvent
    {
        public readonly IEnumerable<string> Fields;

        public FieldsAdded(Guid id, Guid userId, IEnumerable<string> fields)
        {
            Id = id;
            UserId = userId;
            Fields = fields;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
