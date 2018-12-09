using System;

namespace Sds.Osdr.Generic.Domain.Commands.Folders
{
    public interface MoveFolder
    {
        Guid Id { get; }
        int ExpectedVersion { get; }
        Guid SessionId { get; }
        Guid UserId { get; }
        Guid? NewParentId { get; }
    }
}
