using CQRSlite.Events;
using Sds.CqrsLite.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class TargetsChanged : IUserEvent
    {
        public Guid Id { get; set; }
        public IEnumerable<string> Targets { get; set; }

        public TargetsChanged(Guid id, Guid userId, IEnumerable<string> targets)
        {
            Id = id;
            UserId = userId;
            Targets = targets;
        }

        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public Guid UserId { get; set; }
    }
}
