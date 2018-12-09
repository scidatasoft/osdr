using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ModelThumbnailGenerated : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid BlobId { get; }
        string Bucket { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
