using MassTransit;
using System;

namespace Sds.ReactionFileParser.Domain.Commands
{
    public interface ParseFile : CorrelatedBy<Guid>
    {
        string Bucket { get; }
        Guid BlobId { get; }
        Guid Id { get; }
        Guid UserId { get; }
    }
}
