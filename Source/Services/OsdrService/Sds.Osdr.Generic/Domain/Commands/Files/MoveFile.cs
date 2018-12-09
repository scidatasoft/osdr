using System;

namespace Sds.Osdr.Generic.Domain.Commands.Files
{
    public interface MoveFile
    {
        Guid Id { get; }
        Guid UserId { get; }
        Guid? NewParentId { get; }
        int ExpectedVersion { get; }
    }
}
