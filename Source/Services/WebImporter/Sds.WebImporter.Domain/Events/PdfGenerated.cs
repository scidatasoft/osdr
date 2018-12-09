using MassTransit;
using System;

namespace Sds.WebImporter.Domain.Events
{
    public interface PdfGenerated : CorrelatedBy<Guid>
    {
        Guid? BlobId { get; }
        Guid PageId { get; }
        string Title { get; }
        long Lenght { get; }
        string Md5 { get; }
        string Bucket { get; }
        Guid Id { get; set; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; set; }
    }
}
