using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.MetadataStorage.Domain.Events
{
    public interface MetadataGenerated
    {
        Guid Id { get; }
    }
}
