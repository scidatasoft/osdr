using FluentAssertions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class GetVersionOSDR : OsdrWebTest
    {
        public GetVersionOSDR(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact, WebApiTrait(TraitGroup.All)]
        public async Task WebApi_AccessToVersionEndpoint_ReturnsWebApiVersionInformation()
        {
            var response = await JohnApi.GetOSDRVersion();
            response.EnsureSuccessStatusCode();

            var version = await response.Content.ReadAsStringAsync();

            version.Should().NotBeNullOrEmpty();
        }
    }
}
