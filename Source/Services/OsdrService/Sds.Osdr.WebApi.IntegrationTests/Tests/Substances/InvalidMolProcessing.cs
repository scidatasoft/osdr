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
    public class InvalidMolProcessingFixture
    {
        public Guid FileId { get; set; }

        public InvalidMolProcessingFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "ringcount_0.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class InvalidMolProcessing : OsdrWebTest, IClassFixture<InvalidMolProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public InvalidMolProcessing(OsdrWebTestHarness fixture, ITestOutputHelper output, InvalidMolProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidMol_GenerateExpectedFileEntity()
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
				'status': '{FileStatus.Failed}',
				'version': *EXIST*,
				'totalRecords': 1
			}}");
        }
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidMol_GenerateExpectedFileNode()
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
				'ownedBy':'{JohnId}',
				'createdBy': '{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy': '{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'name': '{blobInfo.FileName}',
				'parentId': '{JohnId}',
				'version': *EXIST*,
				'totalRecords': 1
			}}");
            fileNode["images"].Should().BeNullOrEmpty();
        }
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidMol_GenerateExpectedRecordNodesOnlyOne()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());

            recordNodes.Should().HaveCount(1);
        }
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidMol_GenerateExpectedInvalidRecordEntity()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());

            var recordId = recordNodes.First()["id"].ToObject<Guid>();
            recordId.Should().NotBeEmpty();

            var recordEntityResponse = await JohnApi.GetRecordEntityById(recordId);
            var recordEntity = JsonConvert.DeserializeObject<JObject>(await recordEntityResponse.Content.ReadAsStringAsync());
            recordEntity.Should().NotBeEmpty();

            recordEntity.Should().ContainsJson($@"
			{{
				'id': '{recordId}',
				'type': 'Structure',
				'fileId': '{FileId}',
				'ownedBy': '{JohnId}',
				'createdBy': '{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy': '{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'index': 0,
				'status': '{FileStatus.Failed}',
				'message': 'molfile loader: ring bond count is allowed only for queries',
				'version': *EXIST*
			}}");
        }
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidMol_GenerateExpectedInvalidRecordNode()
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
				'subType': 'Structure',
				'ownedBy':'{JohnId}',
				'createdBy':'{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy':'{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'parentId': '{FileId}',
				'version': *EXIST*,
				'status': '{FileStatus.Failed}'
			}}");
        }
    }
}