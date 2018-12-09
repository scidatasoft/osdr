using MassTransit;
using System;

namespace Sds.Imaging.Domain.Events
{
    public interface SourceDeleted : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
        string Bucket { get; } 
    }
}
