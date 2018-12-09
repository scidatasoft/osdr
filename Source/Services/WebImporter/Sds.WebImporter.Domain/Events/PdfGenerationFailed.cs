using MassTransit;
using System;

namespace Sds.WebImporter.Domain.Events
{
    public interface PdfGenerationFailed : CorrelatedBy<Guid>
    {
        string Message { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
