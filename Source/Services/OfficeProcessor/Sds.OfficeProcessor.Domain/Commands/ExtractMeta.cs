using MassTransit;
using System;

namespace Sds.OfficeProcessor.Domain.Commands
{
    public interface ExtractMeta : CorrelatedBy<Guid>
    {
        string Bucket { get; }
        Guid BlobId { get; }
        Guid Id { get; }
        Guid UserId { get; }
    }
}
