using CQRSlite.Domain;
using Sds.Osdr.Generic.Domain.Events.Users;
using System;

namespace Sds.Osdr.Generic.Domain
{
    public class User : AggregateRoot
    {
        /// <summary>
        /// User Id of the person who has created the entity
        /// </summary>
        public Guid CreatedBy { get; private set; }

        /// <summary>
        /// Date and time when entity was created
        /// </summary>
        public DateTimeOffset CreatedDateTime { get; private set; }

        /// <summary>
        /// User Id of person who changed the entity last time
        /// </summary>
        public Guid UpdatedBy { get; protected set; }

        /// <summary>
        /// Date and time when entity was changed last time
        /// </summary>
        public DateTimeOffset UpdatedDateTime { get; protected set; }

        public string DisplayName { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string LoginName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }

        private void Apply(UserCreated e)
        {
            Id = e.Id;
            CreatedBy = e.UserId;
            CreatedDateTime = e.TimeStamp;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            FirstName = e.FirstName;
            LastName = e.LastName;
            DisplayName = e.DisplayName;
            LoginName = e.LoginName;
            Email = e.Email;
            Avatar = e.Avatar;
        }

        private void Apply(UserUpdated e)
        {
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            FirstName = e.FirstName;
            LastName = e.LastName;
            DisplayName = e.LastName;
            Email = e.Email;
            Avatar = e.Avatar;
        }

        private User()
        {
        }

        public User(Guid id, Guid userId, string firstName, string lastName, string displayName, string loginName, string email, string avatar)
        {
            Id = id;
            ApplyChange(new UserCreated(Id, userId, firstName, lastName, displayName, loginName, email, avatar));
        }

        public void Update(Guid id, Guid userId, string newFirstName, string newLastName, string newDisplayName, string newEmail, string newAvatar)
        {
            ApplyChange(new UserUpdated(id, userId, newFirstName, newLastName, newDisplayName, newEmail, newAvatar));
        }
    }
}
