using System;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Records
{
    public interface ChangeStatus
    {
        Guid Id { get; set; }
        RecordStatus Status { get; }
        Guid UserId { get; set; }
    }
}
