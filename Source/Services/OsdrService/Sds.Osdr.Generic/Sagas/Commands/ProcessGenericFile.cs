using System;

namespace Sds.Osdr.Generic.Sagas.Commands
{
    public interface ProcessGenericFile
    {
        Guid Id { get; }
        Guid ParentId { get; }
        string Bucket { get; }
        Guid BlobId { get; }
        Guid UserId { get; }
    }
}
