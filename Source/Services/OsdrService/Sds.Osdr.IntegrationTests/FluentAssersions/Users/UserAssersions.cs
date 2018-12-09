using FluentAssertions;
using FluentAssertions.Collections;
using Sds.Osdr.Generic.Domain;
using System.Collections.Generic;

namespace Sds.Osdr.IntegrationTests.FluentAssersions
{
    public static class UserAssersionsExtensions
    {
        public static void EntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, User user)
        {
            assertions.Subject.Should().NotBeNull();

            assertions.Subject.ShouldAllBeEquivalentTo(new Dictionary<string, object>()
            {
                { "_id", user.Id},
                { "CreatedBy", user.CreatedBy },
                { "CreatedDateTime", user.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", user.UpdatedBy },
                { "UpdatedDateTime", user.UpdatedDateTime.UtcDateTime },
                { "FirstName", user.FirstName },
                { "LastName", user.LastName },
                { "DisplayName", user.DisplayName },
                { "LoginName", user.LoginName },
                { "Email", user.Email },
                { "Avatar", user.Avatar },
                { "Version", user.Version }
            });
        }

        public static void NodeShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, User user)
        {
            assertions.Subject.Should().NotBeNull();

            assertions.Subject.ShouldAllBeEquivalentTo(new Dictionary<string, object>()
            {
                { "_id", user.Id},
                { "Type", "User" },
                { "CreatedBy", user.CreatedBy },
                { "CreatedDateTime", user.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", user.UpdatedBy },
                { "UpdatedDateTime", user.UpdatedDateTime.UtcDateTime },
                { "FirstName", user.FirstName },
                { "LastName", user.LastName },
                { "DisplayName", user.DisplayName },
                { "LoginName", user.LoginName },
                { "Email", user.Email },
                { "Avatar", user.Avatar },
                { "Version", user.Version }
            });
        }
    }
}
