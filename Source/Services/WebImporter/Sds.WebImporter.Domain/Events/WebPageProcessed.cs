using MassTransit;
using System;

namespace Sds.WebImporter.Domain
{
    public interface WebPageProcessed : CorrelatedBy<Guid>
    {
        Guid? BlobId { get; }
        string Bucket { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
