using MassTransit;
using System;

namespace Sds.Osdr.WebPage.Domain.Commands
{
    public interface UpdateWebPage: CorrelatedBy<Guid>
    {
        Guid JsonBlobId { get; set; }
        string Bucket { get; set; }
        Guid Id { get; set; }
        Guid UserId { get; set; }
    }
}
