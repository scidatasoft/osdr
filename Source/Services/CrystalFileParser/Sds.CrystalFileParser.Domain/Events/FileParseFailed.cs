using MassTransit;
using System;

namespace Sds.CrystalFileParser.Domain.Events
{
    public interface FileParseFailed : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        string Message { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
