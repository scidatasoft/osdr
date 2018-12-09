using MassTransit;
using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.CrystalFileParser.Domain.Events
{
    public interface RecordParsed : CorrelatedBy<Guid>
    {
        Guid Id { get; set; }
        Guid FileId { get; }
        string Bucket { get; }
        Guid BlobId { get; }
        long Index { get; }
        IEnumerable<Field> Fields { get; }
        DateTimeOffset TimeStamp { get; }
        Guid UserId { get; }
    }
}
