using Sds.ChemicalStandardizationValidation.Domain.Models;
using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Events
{
    public interface StandardizedPersisted : CorrelatedBy<Guid>
    {
        StandardizedRecord Record { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
