using MassTransit;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.ChemicalFileParser.Domain
{
    public interface FileParsed : CorrelatedBy<Guid>
    {
		long TotalRecords { get; }
        long ParsedRecords  { get; }
		long FailedRecords { get; }
        IEnumerable<string> Fields { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
