using System;

namespace Sds.Osdr.Generic.Domain.Commands.Models
{
    public interface DeleteModel
    {
        Guid Id { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
        bool Force { get; }
    }
}
