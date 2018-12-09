using System;

namespace Sds.Osdr.Generic.Domain.Commands.Users
{
    public interface UpdateUser
    {
        Guid Id { get; }
        string NewDisplayName { get; }
        string NewFirstName { get; }
        string NewLastName { get; }
        string NewEmail { get; }
        string NewAvatar { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
	}
}
