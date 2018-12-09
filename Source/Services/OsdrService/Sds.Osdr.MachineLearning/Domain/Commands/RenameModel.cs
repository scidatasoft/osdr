using System;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface RenameModel
    {
        Guid Id { get; }
        string NewName { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
    }
}
