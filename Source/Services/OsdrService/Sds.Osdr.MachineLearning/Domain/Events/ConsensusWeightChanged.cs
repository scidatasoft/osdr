using CQRSlite.Events;
using Sds.CqrsLite.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class ConsensusWeightChanged : IUserEvent
    {
        public Guid Id { get; set; }
        public double ConsensusWeight { get; set; }

        public ConsensusWeightChanged(Guid id, Guid userId, double consensusWeight)
        {
            Id = id;
            UserId = userId;
            ConsensusWeight = consensusWeight;
        }

        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public Guid UserId { get; set; }
    }
}
