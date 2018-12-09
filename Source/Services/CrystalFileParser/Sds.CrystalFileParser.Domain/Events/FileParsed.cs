using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.CrystalFileParser.Domain.Events
{
    public interface FileParsed : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        long TotalRecords { get; }
        IEnumerable<string> Fields { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
