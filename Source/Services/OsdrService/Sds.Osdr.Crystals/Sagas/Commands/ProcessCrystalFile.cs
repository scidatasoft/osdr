using System;

namespace Sds.Osdr.Crystals.Sagas.Commands
{
    public interface ProcessCrystalFile
    {
        Guid Id { get; set; }
        string Bucket { get; }
        Guid BlobId { get; }
        Guid UserId { get; set; }
    }
}
