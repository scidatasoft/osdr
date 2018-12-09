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
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class FileCreatePublicSharingFixture
    {
        public Guid FileId { get; set; }

        public FileCreatePublicSharingFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            var file = harness.Session.Get<RecordsFile.Domain.RecordsFile>(FileId).Result;
            var response = harness.JohnApi.SetPublicFileEntity(FileId, file.Version, true).Result;
            harness.WaitWhileFileShared(FileId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class FileCreatePublicSharing : OsdrWebTest, IClassFixture<FileCreatePublicSharingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public FileCreatePublicSharing(OsdrWebTestHarness fixture, ITestOutputHelper output, FileCreatePublicSharingFixture initFixture) 
	        : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedFileEntity()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var fileEntityResponse = await UnauthorizedApi.GetFileEntityById(FileId);
            fileEntityResponse.EnsureSuccessStatusCode();
            fileEntityResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
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
			}}");
            fileEntity["properties"].Should().NotBeEmpty();
            fileEntity["properties"].Should().HaveCount(2);
            fileEntity["images"].Should().HaveCount(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedFileNode()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var fileEntityResponse = await UnauthorizedApi.GetNodeById(FileId);
            fileEntityResponse.EnsureSuccessStatusCode();
            fileEntityResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
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
				'type': 'File'
			}}");
            fileEntity["images"].Should().HaveCount(1);
        }

        [Fact(Skip = "Not endpoint api/nodes/shared"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_ListOfSharedFiles_ContainsExpectedFileNode()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var fileEntityResponse = await JohnApi.GetSharedFiles();
            var sharedInfo = JsonConvert.DeserializeObject<JArray>(await fileEntityResponse.Content.ReadAsStringAsync());

            var fileNode = sharedInfo.Last();

            fileNode.Should().NotBeNull();
            fileNode.Should().ContainsJson($@"
			{{
				'id': '{FileId}',
				'type': 'File',
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
				'accessPermissions': {{
					'id': '{FileId}',
					'isPublic': 'True',
					'users': *EXIST*,
					'groups': *EXIST*
				}}
			}}");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedRecordEntity()
        {
            var recordResponse = await UnauthorizedApi.GetNodesById(FileId);
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

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedRecordNode()
        {
            var recordResponse = await UnauthorizedApi.GetNodesById(FileId);
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

            recordNode["images"].Should().NotBeNull();
            recordNode["images"].Should().HaveCount(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedNotFound()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var response = await UnauthorizedApi.GetNodeEntityById(FileId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
            response.StatusCode.ShouldBeEquivalentTo(400);
            response.ReasonPhrase.ShouldAllBeEquivalentTo("Bad Request");
        }
	    
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsRecordNotFound()
        {
            var response = await UnauthorizedApi.GetRecordEntityById(FileId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
            response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
            response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }
    }
}