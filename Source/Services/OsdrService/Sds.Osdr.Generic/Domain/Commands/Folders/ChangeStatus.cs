using MassTransit;
using System;

namespace Sds.Osdr.Generic.Domain.Commands.Folders
{
    public interface ChangeStatus : CorrelatedBy<Guid>
	{
        Guid Id { get; }
        FolderStatus Status { get; }
		Guid UserId { get; }
		int ExpectedVersion { get; }
	}
}
