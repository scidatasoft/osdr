using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class NewUserFixture
    {
        public Guid NewUserId { get; }

        public NewUserFixture(OsdrTestHarness harness)
        {
            NewUserId = harness.CreateUser("John Doe", "John", "Doe", "john", "john@your-company.com", null, harness.JohnId).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class CreateNewUser : OsdrTest, IClassFixture<NewUserFixture>
    {
        private Guid NewUserId { get; }

        public CreateNewUser(OsdrTestHarness fixture, ITestOutputHelper output, NewUserFixture initFixture) : base(fixture, output)
        {
            NewUserId = initFixture.NewUserId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task CreateUser_JohnDoe_RegisterNewUser()
        {
            var user = await Session.Get<User>(NewUserId);

            user.Should().NotBeNull();
            user.ShouldBeEquivalentTo(new
            {
                Id = NewUserId,
                CreatedBy = JohnId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                UpdatedBy = JohnId,
                UpdatedDateTime = DateTimeOffset.UtcNow,
                FirstName = "John",
                LastName = "Doe",
                DisplayName = "John Doe",
                LoginName = "john",
                Email = "john@your-company.com"
            }, options => options
                .ExcludingMissingMembers()
            );
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task CreateUser_JohnDoe_UserEntity()
        {
            var user = await Session.Get<User>(NewUserId);

            var userView = Users.Find(new BsonDocument("_id", NewUserId)).FirstOrDefault() as IDictionary<string, object>;
            userView.Should().EntityShouldBeEquivalentTo(user);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task CreateUser_JohnDoe_UserNode()
        {
            var user = await Session.Get<User>(NewUserId);

            var userNode = Nodes.Find(new BsonDocument("_id", NewUserId)).FirstOrDefault() as IDictionary<string, object>;
            userNode.Should().NodeShouldBeEquivalentTo(user);
        }
    }
}
