using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Commands
{
    public interface DeleteValidation : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
    }
}
