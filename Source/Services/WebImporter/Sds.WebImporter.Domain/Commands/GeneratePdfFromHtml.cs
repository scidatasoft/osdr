using MassTransit;
using System;

namespace Sds.WebImporter.Domain.Commands
{
    public interface GeneratePdfFromHtml : CorrelatedBy<Guid>
    {
        string Bucket { get; }
        string Url { get; }
        Guid Id { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
    }
}
