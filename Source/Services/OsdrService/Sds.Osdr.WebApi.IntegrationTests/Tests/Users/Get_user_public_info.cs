using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class GetUserPublicInfo : OsdrWebTest
    {
        public User JohnDoe { get; set; }
        public GetUserPublicInfo(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            JohnDoe = Session.Get<User>(JohnId).Result;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task UserOperation_GetUserPublicInfo_ReturnsUserInfo()
        {
            var response = await JohnApi.GetUserPublicInfoById(JohnId);
            response.EnsureSuccessStatusCode();
            var user = JObject.Parse(await response.Content.ReadAsStringAsync());

            user.Should().ContainsJson($@"
			{{
				'id': '{JohnDoe.Id}',
                'displayName': '{JohnDoe.DisplayName}',
                'firstName': '{JohnDoe.FirstName}',
                'lastName': '{JohnDoe.LastName}',
                'avatar': '{JohnDoe.Avatar}'
			}}");
        }
		
    }
}