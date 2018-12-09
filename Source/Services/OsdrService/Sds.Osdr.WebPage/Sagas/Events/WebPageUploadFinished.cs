using MassTransit;
using System;

namespace Sds.Osdr.WebPage.Sagas.Events
{
    public interface WebPageUploadFinished : CorrelatedBy<Guid>
    {
        Guid FileId { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
