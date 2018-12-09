using System;

namespace Sds.Osdr.Generic.Domain.Commands.Files
{
    public interface DeleteFile
    {
        Guid Id { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
        bool Force { get; }
    }
}
