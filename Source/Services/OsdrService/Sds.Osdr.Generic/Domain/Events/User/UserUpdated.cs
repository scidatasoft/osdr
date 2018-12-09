using CQRSlite.Events;
using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Users
{
    public class UserUpdated : IEvent, IUserEvent
	{
        public readonly string DisplayName;
        public readonly string FirstName;
        public readonly string LastName;
        public readonly string Email;
        public readonly string Avatar;

        public UserUpdated(Guid id, Guid userId, string firstName, string lastName, string displayName, string email, string avatar)
        {
            Id = id;
            DisplayName = displayName;
            FirstName = firstName;
            LastName = lastName;
            UserId = userId;
            Email = email;
            Avatar = avatar;
        }

        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public Guid UserId { get; }
    }
}
