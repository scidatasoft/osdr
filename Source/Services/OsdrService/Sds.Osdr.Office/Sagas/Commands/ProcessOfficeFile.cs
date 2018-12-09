using System;

namespace Sds.Osdr.Office.Sagas.Commands
{
    public interface ProcessOfficeFile
    {
        Guid Id { get; }
        string Bucket { get; }
        Guid BlobId { get; }
        Guid UserId { get; }
    }
}
