using Sds.CqrsLite.Events;
using Sds.Osdr.Generic.Domain.ValueObjects;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class IssueAdded : IUserEvent
    {
        public Issue Issue { get; private set; }

        public IssueAdded(Guid id, Guid userId, Issue issue)
        {
            Id = id;
            UserId = userId;
            Issue = issue;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
