using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface UpdateModelProperties
    {
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
        Dataset Dataset { get; }
        Property Property { get; }
        IDictionary<string, object> Metadata { get; } 
        double Modi { get; }
        string DisplayMethodName { get; }
    }
}
