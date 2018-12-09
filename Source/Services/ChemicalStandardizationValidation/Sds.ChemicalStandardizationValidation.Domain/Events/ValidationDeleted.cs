using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Events
{
    public interface ValidationDeleted : CorrelatedBy<Guid>
    {
        Guid Id { get; set; }
        Guid UserId { get; set; }
        DateTimeOffset TimeStamp { get; }
    }
}
