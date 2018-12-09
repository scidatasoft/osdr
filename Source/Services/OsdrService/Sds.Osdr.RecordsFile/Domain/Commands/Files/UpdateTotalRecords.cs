using System;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Files
{
    public interface UpdateTotalRecords
    {
        long TotalRecords { get; }
        Guid Id { get; }
        Guid UserId { get; }
    }
}
