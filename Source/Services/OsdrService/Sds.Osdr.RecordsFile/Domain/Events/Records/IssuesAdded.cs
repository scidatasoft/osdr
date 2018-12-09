using Sds.CqrsLite.Events;
using Sds.Osdr.Generic.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class IssuesAdded : IUserEvent
    {
        public readonly IEnumerable<Issue> Issues;

        public IssuesAdded(Guid id, Guid userId, IEnumerable<Issue> issues)
        {
            Id = id;
            UserId = userId;
            Issues = issues;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
