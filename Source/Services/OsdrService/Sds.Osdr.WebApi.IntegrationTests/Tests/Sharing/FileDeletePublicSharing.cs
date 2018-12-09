using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests.Tests.Files
{
    public class FileDeletePublicSharingFixture
    {
        public Guid FileId { get; set; }

        public FileDeletePublicSharingFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            var file = harness.Session.Get<RecordsFile.Domain.RecordsFile>(FileId).Result;
            var response = harness.JohnApi.SetPublicFileEntity(FileId, file.Version, true).Result;
            harness.WaitWhileFileShared(FileId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class FileDeletePublicSharing : OsdrWebTest, IClassFixture<FileDeletePublicSharingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public FileDeletePublicSharing(OsdrWebTestHarness fixture, ITestOutputHelper output, FileDeletePublicSharingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedNotFoundFileEntity()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var response = await UnauthorizedApi.GetFileEntityById(FileId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
//			response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
//			response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }

        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedNotFoundFileNode()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var response = await UnauthorizedApi.GetNodeById(FileId);
            var sharedInfo = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
//			response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
//			response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }

        [Fact(Skip = "Not endpoint api/nodes/shared"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_ListOfSharedFiles_ContainsExpectedNotFoundFileNode()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var fileEntityResponse = await JohnApi.GetSharedFiles();
            var sharedInfo = JsonConvert.DeserializeObject<JArray>(await fileEntityResponse.Content.ReadAsStringAsync());

            foreach (var node in sharedInfo)
            {
                Assert.NotEqual(node["id"].ToObject<Guid>(),
                    FileId);
            }
        }

        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedNotFoundRecordEntity()
        {
            var response = await UnauthorizedApi.GetNodesById(FileId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
//			response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
//			response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }

        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedNotFoundRecordNode()
        {
            var recordResponse = await JohnApi.GetNodesById(FileId);
            var recordNodes = JsonConvert.DeserializeObject<JArray>(await recordResponse.Content.ReadAsStringAsync());

            var recordId = recordNodes.First()["id"].ToObject<Guid>();
            recordId.Should().NotBeEmpty();
			
            var response = await UnauthorizedApi.GetNodeById(recordId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
//			response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
//			response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task BlobRecordSharing_WithUnauthorizeUser_ReturnsExpectedNotFound()
        {
            var response = await UnauthorizedApi.GetBlobRecordEntityById(FileId, BlobId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
//			response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
//			response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }

        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task BlobFileSharing_WithUnauthorizeUser_ReturnsExpectedNotFound()
        {
            var response = await UnauthorizedApi.GetBlobFileEntityById(FileId, BlobId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
//            response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
//            response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }

        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task BlobImageSharing_WithUnauthorizeUser_ReturnsExpectedNotFound()
        {
            var fileResponse = await JohnApi.GetFileEntityById(FileId);
            var file = JObject.Parse(await fileResponse.Content.ReadAsStringAsync());
            var imageId = file["images"].First()["id"].ToObject<Guid>();
			
            var response = await UnauthorizedApi.GetImagesFileEntityById(FileId, imageId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
//            response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
//            response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }

        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task BlobImageRecordSharing_WithUnauthorizeUser_ReturnsExpectedNotFound()
        {
            var nodeRecordResponse = await JohnApi.GetNodesById(FileId);
            var nodeRecord = JArray.Parse(await nodeRecordResponse.Content.ReadAsStringAsync()).First();

            var nodeRecordId = nodeRecord["id"].ToObject<Guid>();
            var recordResponse = await JohnApi.GetRecordEntityById(nodeRecordId);
            var record = JObject.Parse(await recordResponse.Content.ReadAsStringAsync());
            var recordId = record["id"].ToObject<Guid>();
            var imageId = record["images"].First()["id"].ToObject<Guid>();

            var response = await UnauthorizedApi.GetImagesRecordEntityById(recordId, imageId);
            response.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
//            response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
//            response.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }
    }
}