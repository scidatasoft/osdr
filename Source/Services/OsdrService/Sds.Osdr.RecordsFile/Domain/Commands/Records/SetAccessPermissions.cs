using Sds.Osdr.Generic.Domain.ValueObjects;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Records
{
    public interface SetAccessPermissions
    {
        Guid Id { get; set; }
        AccessPermissions Permissions { get; set; }
        Guid UserId { get; set; }
    }
}
