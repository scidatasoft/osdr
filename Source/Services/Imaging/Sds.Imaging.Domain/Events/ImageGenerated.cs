using MassTransit;
using Sds.Imaging.Domain.Models;
using System;

namespace Sds.Imaging.Domain.Events
{
    public interface ImageGenerated : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid BlobId { get; }
        string Bucket { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
        Image Image { get; }
    }
}
