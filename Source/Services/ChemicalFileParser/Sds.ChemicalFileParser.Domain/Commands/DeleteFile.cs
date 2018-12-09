using MassTransit;
using System;

namespace Sds.ChemicalFileParser.Domain.Commands
{
    public interface DeleteFile : CorrelatedBy<Guid>
    {
        Guid Id { get; }
    }
}
