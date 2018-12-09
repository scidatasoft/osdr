using Sds.CqrsLite.Events;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class FieldsUpdated : IUserEvent
    {
        public readonly IEnumerable<Field> Fields;

        public FieldsUpdated(Guid id, Guid userId, IEnumerable<Field> fields)
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
