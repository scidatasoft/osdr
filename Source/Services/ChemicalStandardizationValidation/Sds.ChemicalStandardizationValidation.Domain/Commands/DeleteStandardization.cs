using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Commands
{
    public interface DeleteStandardization : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
    }
}
