using Sds.Osdr.Generic.Domain.ValueObjects;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface GrantAccess
    {
        Guid Id { get; set; }
        AccessPermissions Permissions { get; set; }
        Guid UserId { get; set; }
    }
}
