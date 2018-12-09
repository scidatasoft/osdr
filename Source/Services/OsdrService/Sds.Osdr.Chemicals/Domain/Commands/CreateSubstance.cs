using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Chemicals.Domain.Commands
{
    public interface CreateSubstance
    {
        Guid Id { get; set; }
        Guid FileId { get; }
        string Bucket { get; }
        Guid BlobId { get; }
        long Index { get; }
        IEnumerable<Field> Fields { get; }
        Guid UserId { get; set; }
    }
}
