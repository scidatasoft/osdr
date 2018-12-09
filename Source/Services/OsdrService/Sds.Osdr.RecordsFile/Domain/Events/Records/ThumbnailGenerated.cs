using MassTransit;
using Sds.Imaging.Domain.Models;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public interface ThumbnailGenerated : CorrelatedBy<Guid>
     {
        Guid Id { get; }
        long Index { get; }
        Guid BlobId { get; }
        string Bucket { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
        Image Image { get; }
    }
}
