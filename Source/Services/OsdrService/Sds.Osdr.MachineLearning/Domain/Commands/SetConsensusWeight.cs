using Sds.Osdr.Generic.Domain.ValueObjects;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface SetConsensusWeight
    {
        Guid Id { get; set; }
        double ConsensusWeight { get; set; }
        Guid UserId { get; set; }
    }
}
