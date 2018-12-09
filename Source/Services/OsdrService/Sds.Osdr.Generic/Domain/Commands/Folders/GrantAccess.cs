using Sds.Osdr.Generic.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.Generic.Domain.Commands.Folders
{
    public interface GrantAccess
    {
        Guid Id { get; set; }
        AccessPermissions Permissions { get; set; }
        Guid UserId { get; set; }
    }
}
