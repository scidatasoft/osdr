using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface UpdateModelName
    {
        Guid Id { get; }
        string ModelName { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
