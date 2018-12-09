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
    public class ValidSdfProcessingFixture
    {
        public Guid FileId { get; set; }

        public ValidSdfProcessingFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "drugbank_10_records.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class ValidSdfProcessing : OsdrWebTest, IClassFixture<ValidSdfProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public ValidSdfProcessing(OsdrWebTestHarness fixture, ITestOutputHelper output, ValidSdfProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidSdf_GenerateExpectedFileEntity()
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
				'createdDateTime': *EXIST*,
				'updatedBy': '{JohnId}',
				'updatedDateTime': *EXIST*,
				'parentId': '{JohnId}',
				'name': '{blobInfo.FileName}',
				'status': '{FileStatus.Processed}',
				'version': 8,
				'totalRecords': 2,
				'properties': {{
					'fields': [
						'StdInChI',
						'StdInChIKey',
						'SMILES'
					],
					'chemicalProperties': [
						'MOST_ABUNDANT_MASS',
						'MONOISOTOPIC_MASS',
						'MOLECULAR_WEIGHT',
						'MOLECULAR_FORMULA',
						'SMILES',
						'NonStdInChI',
						'InChIKey',
						'InChI',
						'NonStdInChIKey'
					]
				}}
			}}");
            fileEntity["images"].Should().NotBeNull();
            fileEntity["images"].Should().HaveCount(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidSdf_GenerateExpectedFileNode()
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
				'createdBy': '{JohnId}',
				'createdDateTime': *EXIST*,
				'updatedBy':'{JohnId}',
				'updatedDateTime': *EXIST*,
				'name': '{blobInfo.FileName}',
				'parentId': '{JohnId}',
				'version': *EXIST*,
				'totalRecords': 2
			}}");
            fileNode["images"].Should().NotBeNull();
            fileNode["images"].Should().HaveCount(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidSdf_GenerateExpectedRecordNodeAndRecordEntity()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());

            recordNodes.Should().HaveCount(2);
            var recordIndex = 0;

            foreach (var recordNodesItem in recordNodes)
            {
                var recordId = recordNodesItem["id"].ToObject<Guid>();
                recordId.Should().NotBeEmpty();

                await ValidRecordEntity(recordId, recordIndex);
                await ValidRecordNode(recordId, recordIndex);

                recordIndex++;
            }
        }
        private async Task ValidRecordNode(Guid recordId, int recordIndex)
        {
            var recordNodeResponse = await JohnApi.GetNodeById(recordId);
            var recordNode = JsonConvert.DeserializeObject<JObject>(await recordNodeResponse.Content.ReadAsStringAsync());
            recordNode.Should().NotBeEmpty();
            recordNode.Should().ContainsJson($@"
				{{
 					'id': '{recordId}',
					'type': 'Record',
					'subType': 'Structure',
					'name': {recordIndex},
					'blob': {{
						'bucket': '{JohnId}'
					}},
					'ownedBy': '{JohnId}',
					'createdBy': '{JohnId}',
					'createdDateTime': *EXIST*,
					'updatedBy': '{JohnId}',
					'updatedDateTime': *EXIST*,
					'parentId': '{FileId}',
					'version': *EXIST*,
					'status': '{FileStatus.Processed}',
				}}");
            recordNode["images"].Should().NotBeNull();
            recordNode["images"].Should().HaveCount(1);
        }
        private async Task ValidRecordEntity(Guid recordId, int recordIndex)
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
					'createdDateTime': *EXIST*,
					'updatedBy': '{JohnId}',
					'updatedDateTime': *EXIST*,
					'index': {recordIndex},
					'status': '{FileStatus.Processed}',
					'version': *EXIST*,
					'properties': {{
						'fields': [
							{{ 
								'name': 'StdInChI', 
								'value': 'StdInChI-{recordIndex}'
							}},
							{{
								'name': 'StdInChIKey',
								'value': 'StdInChIKey-{recordIndex}'
							}},
							{{
								'name': 'SMILES',
								'value': 'SMILES-{recordIndex}'
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