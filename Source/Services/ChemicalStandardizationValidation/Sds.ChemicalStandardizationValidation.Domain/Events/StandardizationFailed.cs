using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Events
{
    public interface StandardizationFailed : CorrelatedBy<Guid>
    {
        string Message { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
