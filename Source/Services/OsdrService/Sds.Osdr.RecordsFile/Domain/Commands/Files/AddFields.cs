using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Files
{
    public interface AddFields
    {
        Guid Id { get; }
        IEnumerable<string> Fields { get; }
        Guid UserId { get; }
    }
}
