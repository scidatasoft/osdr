using MassTransit;
using System;

namespace Sds.Osdr.Generic.Sagas.Events
{
    public interface FileProcessed : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid ParentId { get; }
        Guid BlobId { get; }
        string Bucket { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
