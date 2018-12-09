using Sds.Osdr.Generic.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Records
{
    public interface AddIssues
    {
        Guid Id { get; set; }
        IEnumerable<Issue> Issues { get; set; }
        Guid UserId { get; set; }
    }
}
