using MassTransit;
using System;

namespace Sds.WebImporter.Domain.Events
{
    public interface WebPageParseFailed : CorrelatedBy<Guid>
    {
        string Message { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
