using MassTransit;
using System;

namespace Sds.ChemicalFileParser.Domain
{
    public interface FileParseFailed : CorrelatedBy<Guid>
    {
        long TotalRecords { get; }
        long ParsedRecords { get; }
        long FailedRecords { get; }
        string Message { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
