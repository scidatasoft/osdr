using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ModelNamePersisted
    {
        Guid Id { get; }
        string Name { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
