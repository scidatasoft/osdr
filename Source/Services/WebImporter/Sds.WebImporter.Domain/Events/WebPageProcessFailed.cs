using MassTransit;
using System;

namespace Sds.WebImporter.Domain
{
    public interface WebPageProcessFailed : CorrelatedBy<Guid>
    {
        string Message { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; } 
    }
}
