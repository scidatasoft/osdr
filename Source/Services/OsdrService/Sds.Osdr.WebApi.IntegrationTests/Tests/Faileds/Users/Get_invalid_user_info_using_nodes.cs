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
    public class UnauthorizedGetUserInfoUsingNodes : OsdrWebTest
    {
        public UnauthorizedGetUserInfoUsingNodes(OsdrWebTestHarness fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Users, TraitGroup.Failed)]
        public async Task WebApi_GetUserInfoUsingNodesEndpoint_ReturnsError()
        {
            var response = await UnauthorizedApi.GetNodeById(JohnId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
            response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.Forbidden);
        }
    }
}