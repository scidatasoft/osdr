using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Commands
{
    public interface DeleteValidationStandardization : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
    }
}
