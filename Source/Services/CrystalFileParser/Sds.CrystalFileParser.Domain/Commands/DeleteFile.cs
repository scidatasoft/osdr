using MassTransit;
using System;

namespace Sds.CrystalFileParser.Domain.Commands
{
    public interface DeleteFile : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
    }
}
