using System;

namespace Sds.Osdr.Generic.Domain.Commands.Users
{
    public interface CreateUser
    {
        Guid Id { get; set; }
        string DisplayName { get; }
        string FirstName { get; }
        string LastName { get; }
        string LoginName { get; }
        string Email { get; }
        string Avatar { get; }
        Guid UserId { get; }
	}
}
