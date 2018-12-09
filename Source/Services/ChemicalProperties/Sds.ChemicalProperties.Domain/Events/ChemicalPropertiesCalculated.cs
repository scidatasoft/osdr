using Sds.ChemicalProperties.Domain.Models;
using MassTransit;
using System;

namespace Sds.ChemicalProperties.Domain.Events
{
    public interface ChemicalPropertiesCalculated : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
        CalculatedProperties Result { get; }
    }
}
