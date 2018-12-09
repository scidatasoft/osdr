using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Office.Domain.Commands
{
    public interface AddMetadata
    {
        Guid Id { get; set; }
        IEnumerable<Property> Metadata { get; }
        Guid UserId { get; set; }
    }
}
