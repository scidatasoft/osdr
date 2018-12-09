using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Events
{
    public interface StandardizationValidationDeleted : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
