using System;

namespace Sds.Osdr.Chemicals.Sagas.Commands
{
    public interface ProcessChemicalFile
    {
        Guid Id { get; }
        string Bucket { get; }
        Guid BlobId { get; }
        Guid UserId { get; }
    }
}
