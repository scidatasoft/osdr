using MassTransit;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.ChemicalFileParser.Domain
{
    public interface RecordParsed : CorrelatedBy<Guid>
    {
        Guid FileId { get; }
        string Bucket { get; }
        Guid BlobId { get; }
        long Index { get; }
        IEnumerable<Field> Fields { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
