using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ModelPersisted
    {
        Guid Id { get; }
        string Name { get; }
        string Method { get; }
        Guid BlobId { get; }
        string Bucket { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
