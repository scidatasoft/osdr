using MassTransit;
using System;

namespace Sds.ChemicalProperties.Domain.Commands
{
    public interface CalculateChemicalProperties : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        string Bucket { get; }
        Guid BlobId { get; }
        Guid UserId { get; }
    }
}
