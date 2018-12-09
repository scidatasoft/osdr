using Sds.ChemicalStandardizationValidation.Domain.Models;
using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Events
{
    public interface ValidatedStandardizedPersisted : CorrelatedBy<Guid>
    {
        StandardizedValidatedRecord Record { get; }
        Guid Id { get; set; }
        Guid UserId { get; set; }
        DateTimeOffset TimeStamp { get; }
    }
}
