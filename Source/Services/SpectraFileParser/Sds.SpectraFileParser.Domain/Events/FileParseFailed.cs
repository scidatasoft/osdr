using MassTransit;
using System;

namespace Sds.SpectraFileParser.Domain.Events
{
    public interface FileParseFailed : CorrelatedBy<Guid>
    {
        string Message { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
