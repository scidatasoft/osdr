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
    public class ValidCdxProcessingFixture
    {
        public Guid FileId { get; set; }

        public ValidCdxProcessingFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "125_11Mos.cdx", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class ValidCdxProcessing : OsdrWebTest, IClassFixture<ValidCdxProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public ValidCdxProcessing(OsdrWebTestHarness fixture, ITestOutputHelper output, ValidCdxProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidCdx_GenerateExpectedFileEntity()
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
				'version': *EXIST*,
				'totalRecords': 3,
				'properties': *EXIST*
			}}");
            fileEntity["images"].Should().NotBeNull();
            fileEntity["images"].Should().HaveCount(1);
        }
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidCdx_GenerateExpectedFileNode()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
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
				'version': *EXIST*,
				'totalRecords': 3
			}}");
            fileNode["images"].Should().NotBeNull();
            fileNode["images"].Should().HaveCount(1);
        }
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidCdx_GenerateExpectedRecordNode()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());
            recordNodes.Should().HaveCount(3);

            foreach (var recordNodeCurrent in recordNodes)
            {
                var recordId = recordNodeCurrent["id"].ToObject<Guid>();
                recordId.Should().NotBeEmpty();

                await ValidRecordEntity(recordId);
                await ValidRecordNode(recordId);
            }
        }
        private async Task ValidRecordNode(Guid recordId)
        {
            var recordNodeResponse = await JohnApi.GetNodeById(recordId);
            var recordNode = JsonConvert.DeserializeObject<JObject>(await recordNodeResponse.Content.ReadAsStringAsync());
            recordNode.Should().NotBeEmpty();
            recordNode.Should().ContainsJson($@"
				{{
 					'id': '{recordId}',
					'type': 'Record',
					'subType': 'Structure',
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

            recordNode["images"].Should().NotBeNull();
            recordNode["images"].Should().HaveCount(1);
        }
        private async Task ValidRecordEntity(Guid recordId)
        {
            var recordEntityResponse = await JohnApi.GetRecordEntityById(recordId);
            var recordEntity = JsonConvert.DeserializeObject<JObject>(await recordEntityResponse.Content.ReadAsStringAsync());
            recordEntity.Should().NotBeEmpty();

            recordEntity.Should().ContainsJson($@"
				{{
					'id': '{recordId}',
					'type': 'Structure',
					'fileId': '{FileId}',
					'blob': {{
						'bucket': '{JohnId}',
					}},
					'ownedBy': '{JohnId}',
					'createdBy': '{JohnId}',
					'createdDateTime': '{DateTime.UtcNow}',
					'updatedBy': '{JohnId}',
					'updatedDateTime': '{DateTime.UtcNow}',
					'status': '{FileStatus.Processed}',
					'version': *EXIST*,
					'properties': {{
						'fields': [
							{{ 
								'name': 'StdInChI', 
								'value': 'InChI=1S/C9H8O4/c1-6(10)13-8-5-3-2-4-7(8)9(11)12/h2-5H,1H3,(H,11,12)'
							}},
							{{
								'name': 'StdInChIKey',
								'value': 'BSYNRYMUTXBXSQ-UHFFFAOYSA-N'
							}},
							{{
								'name': 'SMILES',
								'value': 'CC(OC1=C(C(=O)O)C=CC=C1)=O'
							}}
						],
						'chemicalProperties': *EXIST*
					}}	
				}}");
            recordEntity["images"].Should().NotBeNull();
            recordEntity["images"].Should().HaveCount(1);
        }
    }
}