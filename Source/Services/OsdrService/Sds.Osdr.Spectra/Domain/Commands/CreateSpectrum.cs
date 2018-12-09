using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Spectra.Domain.Commands
{
    public interface CreateSpectrum
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
