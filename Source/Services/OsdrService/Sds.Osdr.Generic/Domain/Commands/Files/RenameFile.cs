using System;

namespace Sds.Osdr.Generic.Domain.Commands.Files
{
    public interface RenameFile
    {
        Guid Id { get; }
        string NewName { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
    }
}
