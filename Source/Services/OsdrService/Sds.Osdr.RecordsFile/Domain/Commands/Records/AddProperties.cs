using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Records
{
    public interface AddProperties
    {
        Guid Id { get; }
        IEnumerable<Property> Properties { get; }
        Guid UserId { get; }
    }
}
