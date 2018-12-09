using FluentAssertions;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests;
using Sds.Osdr.Domain.BddTests.Extensions;
using Sds.Osdr.Generic.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class CreateNewUser : OsdrTest
    {
        private Guid NewUserId { get; }

        public CreateNewUser(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            NewUserId = NewId.NextGuid();
            Harness.CreateUser(NewUserId, "John Doe", "John", "Doe", "john", "john@your-company.com", null, JohnId).Wait();
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
