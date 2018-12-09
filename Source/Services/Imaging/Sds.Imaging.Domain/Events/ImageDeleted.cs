using MassTransit;
using System;

namespace Sds.Imaging.Domain.Events
{
    public interface ImageDeleted : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
        string Bucket { get; }
    }
}
