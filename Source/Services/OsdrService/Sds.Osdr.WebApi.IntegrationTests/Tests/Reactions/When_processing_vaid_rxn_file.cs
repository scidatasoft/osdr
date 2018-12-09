using FluentAssertions;
using FluentAssertions.Primitives;
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
    public class ValidRxnProcessingFixture
    {
        public Guid FileId { get; set; }

        public ValidRxnProcessingFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "10001.rxn", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class ValidRnxProcessing : OsdrWebTest, IClassFixture<ValidRxnProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public ValidRnxProcessing(OsdrWebTestHarness fixture, ITestOutputHelper output, ValidRxnProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_ValidRnx_GenerateExpectedFileEntity()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var fileEntityResponse = await JohnApi.GetFileEntityById(FileId);
            var fileEntity = JsonConvert.DeserializeObject<JObject>(await fileEntityResponse.Content.ReadAsStringAsync());

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
				'version': *EXIST*,
				'totalRecords': 1,
				'properties': {{
					'fields': [
						'Field1',
						'Field2'
					]
				}}
			}}");
            fileEntity["images"].Should().NotBeNull();
            fileEntity["images"].Should().HaveCount(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_ValidRnx_GenerateExpectedFileNode()
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
				'ownedBy': '{JohnId}',
				'createdBy':'{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy': '{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'name': '{blobInfo.FileName}',
				'parentId': '{JohnId}',
				'version': *EXIST*,
				'totalRecords': 1
			}}");
            fileNode["images"].Should().NotBeNull();
            fileNode["images"].Should().HaveCount(1);
        }
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_ValidRnx_GenerateExpectedRecordNodeOnlyOne()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());

            recordNodes.Should().HaveCount(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_ValidRnx_GenerateExpectedRecordEntity()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());
            var recordId = recordNodes.First()["id"].ToObject<Guid>();
            recordId.Should().NotBeEmpty();
            recordId.Should().Should().BeOfType<GuidAssertions>();

            var recordEntityResponse = await JohnApi.GetRecordEntityById(recordId);
            var recordEntity = JsonConvert.DeserializeObject<JObject>(await recordEntityResponse.Content.ReadAsStringAsync());
            recordEntity.Should().NotBeNull();

            recordEntity.Should().ContainsJson($@"
			{{
				'id': '{recordId}',
				'type': 'Reaction',
				'fileId': '{FileId}',
				'blob': {{
					'bucket': '{JohnId}',
				}},
				'ownedBy': '{JohnId}',
				'createdBy': '{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy': '{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'index': 0,
				'status': '{FileStatus.Processed}',
				'version': *EXIST*,
				'properties': {{
					'fields': [
						{{ 
							'name': 'Field1', 
							'value': 'Value1'
						}},
						{{
							'name': 'Field2',
							'value': 'Value2'
						}}
					]
				}}	
			}}");
            recordEntity["images"].Should().NotBeNull();
            recordEntity["images"].Should().HaveCount(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_ValidRnx_GenerateExpectedRecordNode()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());
            var recordId = recordNodes.First()["id"].ToObject<Guid>();
            recordId.Should().NotBeEmpty();
            recordId.Should().Should().BeOfType<GuidAssertions>();

            var recordNodeResponse = await JohnApi.GetNodeById(recordId);
            var recordNode = JsonConvert.DeserializeObject<JObject>(await recordNodeResponse.Content.ReadAsStringAsync());

            recordNode.Should().ContainsJson($@"
			{{
 				'id': '{recordId}',
				'type': 'Record',
				'subType': 'Reaction',
				'name': 0,
				'blob': {{
					'bucket': '{JohnId}'
				}},
				'ownedBy': '{JohnId}',
				'createdBy': '{JohnId}',
				'createdDateTime': '{DateTime.UtcNow}',
				'updatedBy': '{JohnId}',
				'updatedDateTime': '{DateTime.UtcNow}',
				'parentId': '{FileId}',
				'version': *EXIST*,
				'status': '{FileStatus.Processed}',
			}}");
            recordNode["images"].Should().NotBeNull();
            recordNode["images"].Should().HaveCount(1);
        }
    }
}