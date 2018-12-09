using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests.Tests.Streams
{
    public class GetStramAllPublicAggregatesFixture
    {
        public Guid FileId { get; set; }

        public GetStramAllPublicAggregatesFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            var response = harness.JohnApi.SetPublicFileEntity(FileId, 9, true).Result;
            harness.WaitWhileFileShared(FileId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class GetStreamAllPublicAggregate : OsdrWebTest, IClassFixture<GetStramAllPublicAggregatesFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public GetStreamAllPublicAggregate(OsdrWebTestHarness fixture, ITestOutputHelper output, GetStramAllPublicAggregatesFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Stream)]
        public async Task Stream_GetStream_ExpectedFullStream()
        {
            var fileResponse = await JohnApi.GetFileEntityById(FileId);
            var file = JToken.Parse(await fileResponse.Content.ReadAsStringAsync());
            var version = file["version"].ToObject<int>();

            var events = new List<string>()
            {
                "PermissionsChanged",
                "StatusChanged",
                "AggregatedPropertiesAdded",
                "ImageAdded",
                "FieldsAdded",
                "TotalRecordsUpdated",
                "StatusChanged",
                "RecordsFileCreated",
                "FileCreated"
            };
            var response = await JohnApi.GetStreamFileEntityById(FileId, -1, -1);
            var streams = JArray.Parse(await response.Content.ReadAsStringAsync());
            streams.Should().NotBeEmpty();
            streams.Should().HaveCount(9);

            for (var i = 0; i < version; i++)
            {
                var @event = streams[i];

                events.Should().Contain(new[] { @event["name"].ToObject<string>() });

                var eventVersion = version - i;
                
                @event.Should().ContainsJson($@"
			    {{
                    'namespace': *EXIST*,
                    'event': {{
                        'id': '{FileId}',
                        'userId': '{JohnId}',
                        'version': {eventVersion}
                    }}
			    }}");
            }
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Stream)]
        public async Task Stream_GetStream_ExpectedOneStream()
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
        public async Task Stream_GetStreamUnauthorizedUser_ExpectedOneStream()
        {
            var response = await UnauthorizedApi.GetStreamFileEntityById(FileId, 0, 1);
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
    }
}