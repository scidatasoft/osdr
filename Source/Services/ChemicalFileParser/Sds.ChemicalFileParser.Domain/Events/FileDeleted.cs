using System;

namespace Sds.ChemicalFileParser.Domain
{
    public interface FileDeleted
    {
        Guid Id { get; }

        DateTimeOffset TimeStamp { get; }
    }
}
