using MassTransit;
using System;


namespace Sds.ChemicalProperties.Domain.Commands
{
    public interface DeleteChemicalProperties : CorrelatedBy<Guid>
    {
        Guid Id { get; set; }
        Guid UserId { get; set; }
    }
}
