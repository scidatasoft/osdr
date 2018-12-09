using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface SetTargets
    {
        Guid Id { get; }
        Guid UserId { get; }
        IEnumerable<string> Targets { get; }
    }
}
