using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Records
{
    public interface UpdateFields
    {
        IList<Field> Fields { get; }
        Guid Id { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
    }
}
