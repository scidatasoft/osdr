using FluentAssertions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class UnathorizedGetUserInfoMe : OsdrWebTest
    {
        public UnathorizedGetUserInfoMe(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Users, TraitGroup.NotAuthorized)]
        public async Task WebApi_GetUserInfoUsingMeEndpoint_ReturnsError()
        {
            var response = await UnauthorizedApi.GetUserMe();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}