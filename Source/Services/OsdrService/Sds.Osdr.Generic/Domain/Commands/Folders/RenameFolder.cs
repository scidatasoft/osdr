using System;

namespace Sds.Osdr.Generic.Domain.Commands.Folders
{
    public interface RenameFolder
    {
        Guid Id { get; }
        string NewName { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
    }
}
