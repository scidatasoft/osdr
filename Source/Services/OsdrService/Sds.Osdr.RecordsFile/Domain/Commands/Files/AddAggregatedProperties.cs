using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Files
{
    public interface AddAggregatedProperties
    {
        Guid Id { get; }
        IEnumerable<string> Properties { get; }
        Guid UserId { get; }
    }
}
