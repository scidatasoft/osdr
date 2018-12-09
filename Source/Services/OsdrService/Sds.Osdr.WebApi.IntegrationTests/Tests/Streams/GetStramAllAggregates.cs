using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests.Tests.Streams
{
    public class GetStramAllAggregatesFixture
    {
        public Guid FileId { get; set; }

        public GetStramAllAggregatesFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class GetStramAllAggregates : OsdrWebTest, IClassFixture<GetStramAllAggregatesFixture>
    {
        private Guid BlobId => GetBlobId(FileId);
        private Guid FileId { get; }
        
        public GetStramAllAggregates(OsdrWebTestHarness fixture, ITestOutputHelper output, GetStramAllAggregatesFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Stream)]
        public async Task Stream_UseJohn_ReturnsExpectedStreamContent()
        {
            var response = await JohnApi.GetStreamFileEntityById(FileId, 0, 1);
            var streams = JArray.Parse(await response.Content.ReadAsStringAsync());
            streams.Should().NotBeEmpty();
            streams.Should().HaveCount(1);

            var stream = streams.First();
            stream.Should().ContainsJson($@"
			{{
                'name': 'FileCreated',
                'namespace': 'Sds.Osdr.Generic.Domain.Events.Files',
                'event': {{
                    'bucket': '{JohnId.ToString()}',
                    'blobId': '{BlobId}',
                    'parentId': '{JohnId}',
                    'fileName': 'Aspirin.mol',
                    'fileStatus': 'Loaded',
                    'fileType': 'Records',
                    'id': '{FileId}',
                    'userId': '{JohnId}',
                    'version': 1
                }}
			}}");
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Stream)]
        public async Task Stream_UseJane_ReturnProhibitedAccess()
        {
            var response = await JaneApi.GetStreamFileEntityById(FileId, 0, 1);
            response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.Forbidden);
            response.ReasonPhrase.ShouldAllBeEquivalentTo("Forbidden");
        }
    }
}