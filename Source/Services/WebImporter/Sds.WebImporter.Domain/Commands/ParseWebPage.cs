using MassTransit;
using System;

namespace Sds.WebImporter.Domain.Commands
{
    public interface ParseWebPage : CorrelatedBy<Guid>
    {
        string Bucket { get; }
        Guid BlobId { get; }
        Guid Id { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
    }
}