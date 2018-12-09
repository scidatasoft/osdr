using MassTransit;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Files
{
    public interface AggregateProperties : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        //string Bucket { get; }
        //Guid BlobId { get; }
        Guid UserId { get; }
    }
}
