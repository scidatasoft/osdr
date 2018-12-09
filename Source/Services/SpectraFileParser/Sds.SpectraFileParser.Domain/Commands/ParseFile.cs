using MassTransit;
using System;

namespace Sds.SpectraFileParser.Domain.Commands
{
    public interface ParseFile : CorrelatedBy<Guid>
    {
        string Bucket { get; }
        Guid BlobId { get; }
        Guid Id { get; }
        Guid UserId { get; }
    }
}
