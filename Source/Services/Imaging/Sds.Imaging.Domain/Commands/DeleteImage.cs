using MassTransit;
using System;

namespace Sds.Imaging.Domain.Commands
{
    public interface DeleteImage : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        string Bucket { get; }
    }
}
