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
    public class UnauthorizedGetUserInfoUsingEntities : OsdrWebTest
    {
        public UnauthorizedGetUserInfoUsingEntities(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Users, TraitGroup.Failed)]
        public async Task WebApi_GetUserInfoUsingEntitiesEndpoint_ReturnsError()
        {
            var response = await UnauthorizedApi.GetUserEntityById(JohnId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
            response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.Forbidden);
        }
    }
}