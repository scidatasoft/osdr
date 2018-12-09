using System;

namespace Sds.Osdr.Spectra.Sagas.Commands
{
    public interface ProcessSpectralFile
    {
        Guid Id { get; set; }
        string Bucket { get; }
        Guid BlobId { get; }
        Guid UserId { get; set; }
    }
}
