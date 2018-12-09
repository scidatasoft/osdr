using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Events.Files
{
    public interface AddedFieldsPersisted
    {
        Guid Id { get; }
        IEnumerable<string> Fields { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
