using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Events
{
    public interface ValidationFailed : CorrelatedBy<Guid>
    {
        string Message { get; }
        Guid Id { get; set; }
        Guid UserId { get; set; }
        DateTimeOffset TimeStamp { get; }
    }
}
