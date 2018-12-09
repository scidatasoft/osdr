using MassTransit;
using Sds.Imaging.Domain.Models;
using System;

namespace Sds.Imaging.Domain.Events
{
    public interface ImageGenerationFailed : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        DateTimeOffset TimeStamp { get;}
        Guid UserId { get; }
        Image Image { get; }
    }
}
