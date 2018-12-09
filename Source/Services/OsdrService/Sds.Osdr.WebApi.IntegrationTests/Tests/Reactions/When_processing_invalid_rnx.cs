using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class InvalidRxnProcessingFixture
    {
        public Guid FileId { get; set; }

        public InvalidRxnProcessingFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "empty.rxn", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class InvalidRnxProcessing : OsdrWebTest, IClassFixture<InvalidRxnProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public InvalidRnxProcessing(OsdrWebTestHarness fixture, ITestOutputHelper output, InvalidRxnProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_InvalidRnx_GenerateExpectedFileEntity()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var fileEntityResponse = await JohnApi.GetFileEntityById(FileId);
            var fileEntity = JsonConvert.DeserializeObject<JObject>(await fileEntityResponse.Content.ReadAsStringAsync());

            fileEntity.Should().ContainsJson($@"
			{{
				'id': '{FileId}',
				'subType': '{FileType.Records}',
				'ownedBy': '{JohnId}',
				'createdBy': '{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy': '{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'parentId': '{JohnId}',
				'name': '{blobInfo.FileName}',
				'status': '{FileStatus.Failed}',
				'version': *EXIST*
			}}");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_InvalidRnx_GenerateExpectedFileNode()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var fileNodeResponse = await JohnApi.GetNodeById(FileId);
            var fileNode = JsonConvert.DeserializeObject<JObject>(await fileNodeResponse.Content.ReadAsStringAsync());

            fileNode.Should().ContainsJson($@"
			{{
				'id': '{FileId}',
				'type': 'File',
				'subType': 'Records',
				'blob': {{
					'id': '{blobInfo.Id}',
					'bucket': '{JohnId}',
					'length': {blobInfo.Length},
					'md5': '{blobInfo.MD5}'
				}},
				'status': '{FileStatus.Failed}',
				'ownedBy': '{JohnId}',
				'createdBy':'{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy': '{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'name': '{blobInfo.FileName}',
				'parentId': '{JohnId}',
				'version': *EXIST*
			}}");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_InvalidRnx_GenerateExpectedRecoedNodesOnlyEmpty()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());
            recordNodes.Should().BeNullOrEmpty();
        }
    }
}