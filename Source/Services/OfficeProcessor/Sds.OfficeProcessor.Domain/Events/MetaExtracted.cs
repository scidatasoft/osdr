using MassTransit;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.OfficeProcessor.Domain.Events
{
    public interface MetaExtracted : CorrelatedBy<Guid>
    {
        string Bucket { get; }
        Guid BlobId { get; }
        IEnumerable<Property> Meta { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
