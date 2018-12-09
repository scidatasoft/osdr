using MassTransit;
using System;

namespace Sds.Osdr.Generic.Domain.Commands.Folders
{
    public interface DeleteFolder : CorrelatedBy<Guid>
    {
		Guid Id { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
        bool Force { get; }
    }
}
