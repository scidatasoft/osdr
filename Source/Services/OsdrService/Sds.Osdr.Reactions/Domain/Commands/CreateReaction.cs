using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Reactions.Domain.Commands
{
    public interface CreateReaction
    {
        Guid Id { get; }
        Guid FileId { get; }
        string Bucket { get; }
        Guid BlobId { get; }
        long Index { get; }
        IEnumerable<Field> Fields { get; }
        Guid UserId { get; }
    }
}
