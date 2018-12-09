using MassTransit;
using System;

namespace Sds.Imaging.Domain.Commands
{
    public interface DeleteSource : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        string Bucket { get; }
    }
}
