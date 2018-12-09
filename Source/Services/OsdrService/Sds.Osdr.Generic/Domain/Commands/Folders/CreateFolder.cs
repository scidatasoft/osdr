using MassTransit;
using System;

namespace Sds.Osdr.Generic.Domain.Commands.Folders
{
    public interface CreateFolder : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid ParentId { get; }
        string Name { get; }
        Guid UserId { get; }
        Guid SessionId { get; }
	}
}
