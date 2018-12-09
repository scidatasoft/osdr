using MassTransit;
using System;

namespace Sds.OfficeProcessor.Domain.Events
{
    public interface ConvertedToPdf : CorrelatedBy<Guid>
    {
        string Bucket { get; }
        Guid BlobId { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
