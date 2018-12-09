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
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class UploadValidCsvFixture
    {
        public Guid FileId { get; set; }

        public UploadValidCsvFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessFile(harness.JohnId.ToString(), "FocusSynthesis_InStock.csv", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class UploadValidCsv : OsdrWebTest, IClassFixture<UploadValidCsvFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public UploadValidCsv(OsdrWebTestHarness fixture, ITestOutputHelper output, UploadValidCsvFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Tabular)]
        public async Task TabularUpload_ValidCsv_GenerateExpectedFileEntity()
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
				'subType': '{FileType.Tabular}',
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
            fileEntity["images"].Should().NotBeNull();
            fileEntity["images"].Should().HaveCount(3);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Tabular)]
        public async Task TabularUpload_ValidCsv_GenerateExpectedFileNode()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var fileNodeResponse = await JohnApi.GetNodeById(FileId);
            var fileNode = JsonConvert.DeserializeObject<JObject>(await fileNodeResponse.Content.ReadAsStringAsync());
            fileNode.Should().ContainsJson($@"
			{{
				'id': '{FileId}',
				'type': 'File',
				'subType': 'Tabular',
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
            fileNode["images"].Should().NotBeNull();
            fileNode["images"].Should().HaveCount(3);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Tabular)]
        public async Task TabularUpload_ValidCsv_GenerateExpectedRecordNodeIsEmpty()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());

            recordNodes.Should().HaveCount(0);
        }
    }
}