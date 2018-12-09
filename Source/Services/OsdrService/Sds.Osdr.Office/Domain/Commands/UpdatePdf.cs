using System;

namespace Sds.Osdr.Office.Domain.Commands
{
    public interface UpdatePdf
    {
        Guid Id { get; }
        Guid UserId { get; }
        Guid BlobId { get; }
        string Bucket { get; }
    }
}
