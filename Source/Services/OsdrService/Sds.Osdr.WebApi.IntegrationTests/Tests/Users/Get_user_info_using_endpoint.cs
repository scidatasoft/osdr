﻿using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class GetUserInfoUsingEndpoint : OsdrWebTest
    {
        public User JohnDoe { get; set; }
        public GetUserInfoUsingEndpoint(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            JohnDoe = Session.Get<User>(JohnId).Result;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task UserOperation_GetUserInfoUsingUsersEndpoint_ReturnsFullUserInfo()
        {
            var response = await JohnApi.GetUserById(JohnId);
            response.EnsureSuccessStatusCode();

            var user = JObject.Parse(await response.Content.ReadAsStringAsync());

            user.Should().ContainsJson($@"
			{{
				'id': '{JohnDoe.Id}',
                'displayName': '{JohnDoe.DisplayName}',
                'firstName': '{JohnDoe.FirstName}',
                'lastName': '{JohnDoe.LastName}',
                'loginName': '{JohnDoe.LoginName}',
                'email': '{JohnDoe.Email}',
                'avatar': '{JohnDoe.Avatar}',
                'version': '{JohnDoe.Version}'
			}}");
        }

        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task UserOperation_GetUserInfoByInapproprieateUser_ReturnsForbiddenAccess()
        {
            var response = await JaneApi.GetUserById(JohnId);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task UserOperation_GetUserInfoByUnauthorizedUser_ReturnsUnauthorizedAccess()
        {
            var response = await UnauthorizedApi.GetUserById(JohnId);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task UserOperation_GetNodesMeByUnauthorizedUser_ReturnsUnauthorizedAccess()
        {
            var response = await UnauthorizedApi.GetNodesMe();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}