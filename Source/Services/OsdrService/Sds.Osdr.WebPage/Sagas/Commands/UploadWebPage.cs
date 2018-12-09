using MassTransit;
using System;

namespace Sds.Osdr.WebPage.Sagas.Commands
{
    public interface UploadWebPage : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        string Bucket { get; }
        string Url { get; }
        Guid ParentId { get; }
        Guid UserId { get; }
    }
}
