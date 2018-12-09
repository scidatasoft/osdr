using FluentAssertions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests.Tests
{
    [Collection("OSDR Test Harness")]
    public class DummyAuthentication : OsdrWebTest
    {
        public DummyAuthentication(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.DummyAuthentication, TraitGroup.NotAuthorized)]
        public async Task WebApi_NotAuthorizedAccessToProtectedResource_ReturnsUnauthorizedResponse()
        {
            var response = await UnauthorizedApi.GetNodesMe();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.DummyAuthentication)]
        public async Task WebApi_AuthorizedAccessToProtectedResource_ReturnsSuccessResponse()
        {
            var response = await JohnApi.GetNodesMe();
            response.EnsureSuccessStatusCode();
        }
    }
}
