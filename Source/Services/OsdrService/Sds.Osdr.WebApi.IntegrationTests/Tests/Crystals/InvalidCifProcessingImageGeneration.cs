using FluentAssertions;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.Generic.Domain;
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

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class InvalidCifProcessingImageGenerationFixture
    {
        public Guid FileId { get; set; }

        public InvalidCifProcessingImageGenerationFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "InvalidImageGeneration.cif", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class InvalidCifProcessingImageGeneration : OsdrWebTest, IClassFixture<InvalidCifProcessingImageGenerationFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public InvalidCifProcessingImageGeneration(OsdrWebTestHarness fixture, ITestOutputHelper output, InvalidCifProcessingImageGenerationFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Crystal)]
        public async Task CrystalProcessing_InvalidMol_GenerateExpectedFileEntity()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var fileEntityResponse = await JohnApi.GetFileEntityById(FileId);
            var fileEntity = JsonConvert.DeserializeObject<JObject>(await fileEntityResponse.Content.ReadAsStringAsync());
            fileEntity.Should().NotBeNull();

            fileEntity.Should().ContainsJson($@"
			{{
				'id': '{FileId}',
				'blob': {{
					'id': '{blobInfo.Id}',
					'bucket': '{JohnId}',
					'length': {blobInfo.Length},
					'md5': '{blobInfo.MD5}'
				}},
				'subType': '{FileType.Records}',
				'ownedBy': '{JohnId}',
				'createdBy': '{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy': '{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'parentId': '{JohnId}',
				'name': '{blobInfo.FileName}',
				'status': '{FileStatus.Processed}',
				'version': *EXIST*
			}}");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Crystal)]
        public async Task CrystalProcessing_InvalidMol_GenerateExpectedFileNode()
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
				'status': '{FileStatus.Processed}',
				'ownedBy':'{JohnId}',
				'createdBy': '{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy': '{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'name': '{blobInfo.FileName}',
				'parentId': '{JohnId}',
				'version': *EXIST*
			}}");

        }
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Crystal)]
        public async Task CrystalProcessing_InvalidMol_GenerateExpectedRecordNodesOnlyEmpty()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());
            var recordId = recordNodes.First()["id"].ToObject<Guid>();
            recordId.Should().NotBeEmpty();

            var recordNodeResponse = await JohnApi.GetNodeById(recordId);
            var recordNode = JsonConvert.DeserializeObject<JObject>(await recordNodeResponse.Content.ReadAsStringAsync());
            recordNode.Should().NotBeEmpty();

            recordNode.Should().ContainsJson($@"
			{{
 				'id': '{recordId}',
				'type': 'Record',
				'subType': 'Crystal',
				'name': 0,
				'blob': {{
					'bucket': '{JohnId}'
				}},
				'ownedBy':'{JohnId}',
				'createdBy':'{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy':'{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'parentId': '{FileId}',
				'version': *EXIST*,
				'status': '{FileStatus.Processed}',
			}}");

            recordNode["images"].Should().BeNull();
        }
    }
}