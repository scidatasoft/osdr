using MassTransit;
using System;

namespace Sds.ChemicalFileParser.Domain
{
    public interface RecordParseFailed : CorrelatedBy<Guid>
    {
        Guid FileId { get; }
        long Index { get; }
        string Message { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
