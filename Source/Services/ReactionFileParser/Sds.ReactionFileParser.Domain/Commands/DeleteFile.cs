using MassTransit;
using System;

namespace Sds.ReactionFileParser.Domain.Commands
{
    public interface DeleteFile : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
    }
}
