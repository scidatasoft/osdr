using Sds.Osdr.IntegrationTests.Traits;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class UnauthorizedGetUserInfoUsingUsers : OsdrWebTest
    {
        public UnauthorizedGetUserInfoUsingUsers(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact(Skip = "Not implemented"), WebApiTrait(TraitGroup.All, TraitGroup.Users, TraitGroup.Failed)]
        public async Task WebApi_GetUserInfoUsingUsersEndpoint_ReturnsError()
        {
            //var response = await Api.GetUserById(UnauthorizedUserId);
            //response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
            //response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
            //response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }
    }
}