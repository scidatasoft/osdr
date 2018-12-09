using System;

namespace Sds.ChemicalFileParser.Domain
{
    public interface RecordDeleted
    {
        Guid FileId { get; }
        Guid Id { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
