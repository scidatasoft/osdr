using System;

namespace Sds.Osdr.Generic.Domain.Commands.Files
{
    public interface ChangeStatus
    {
        Guid Id { get; set; }
        FileStatus Status { get; }
        Guid UserId { get; set; }
    }
}
