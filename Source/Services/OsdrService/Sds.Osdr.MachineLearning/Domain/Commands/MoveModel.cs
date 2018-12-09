using System;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface MoveModel
    {
        Guid Id { get; }
        Guid UserId { get; }
        Guid? NewParentId { get; }
        int ExpectedVersion { get; }
    }
}
