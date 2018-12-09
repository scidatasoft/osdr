using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.SpectraFileParser.Domain.Events
{
    public interface FileParsed : CorrelatedBy<Guid>
    {
        long TotalRecords { get; }
        IEnumerable<string> Fields { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
