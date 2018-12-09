using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests.Tests.Files
{
    public class CreatePublicLinkForRecordImageFixture
    {
        public Guid FileId { get; set; }

        public CreatePublicLinkForRecordImageFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            var file = harness.Session.Get<RecordsFile.Domain.RecordsFile>(FileId).Result;
            var response = harness.JohnApi.SetPublicFileEntity(FileId, file.Version, true).Result;
            harness.WaitWhileFileShared(FileId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class CreatePublicLinkForRecordImage : OsdrWebTest, IClassFixture<CreatePublicLinkForRecordImageFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public CreatePublicLinkForRecordImage(OsdrWebTestHarness fixture, ITestOutputHelper output, CreatePublicLinkForRecordImageFixture initFixture) 
	        : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedImageRecord()
		{
			var nodeRecordResponse = await JohnApi.GetNodesById(FileId);
			var nodeRecord = JArray.Parse(await nodeRecordResponse.Content.ReadAsStringAsync()).First();

			var nodeRecordId = nodeRecord["id"].ToObject<Guid>();
			var recordResponse = await JohnApi.GetRecordEntityById(nodeRecordId);
			var record = JObject.Parse(await recordResponse.Content.ReadAsStringAsync());
			var recordId = record["id"].ToObject<Guid>();
			var imageId = record["images"].First()["id"].ToObject<Guid>();

			var blobResponse = await UnauthorizedApi.GetImagesRecordEntityById(recordId, imageId);
			blobResponse.EnsureSuccessStatusCode();
			blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
			blobResponse.Content.Headers.ContentType.MediaType.ShouldBeEquivalentTo("application/octet-stream");
			blobResponse.Content.Headers.ContentLength.ShouldBeEquivalentTo(10998);
			blobResponse.Content.Headers.ContentDisposition.FileName.ShouldBeEquivalentTo("Aspirin.mol.svg");
		}

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithAuthorizeUser_ReturnsExpectedImageRecord()
		{
			var nodeRecordResponse = await JohnApi.GetNodesById(FileId);
			var nodeRecord = JArray.Parse(await nodeRecordResponse.Content.ReadAsStringAsync()).First();

			var nodeRecordId = nodeRecord["id"].ToObject<Guid>();
			var recordResponse = await JohnApi.GetRecordEntityById(nodeRecordId);
			var record = JObject.Parse(await recordResponse.Content.ReadAsStringAsync());
			var recordId = record["id"].ToObject<Guid>();
			var imageId = record["images"].First()["id"].ToObject<Guid>();

			var blobResponse = await JohnApi.GetImagesRecordEntityById(recordId, imageId);
			blobResponse.EnsureSuccessStatusCode();
			blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
			blobResponse.Content.Headers.ContentType.MediaType.ShouldBeEquivalentTo("application/octet-stream");
			blobResponse.Content.Headers.ContentLength.ShouldBeEquivalentTo(10998);
			blobResponse.Content.Headers.ContentDisposition.FileName.ShouldBeEquivalentTo("Aspirin.mol.svg");
		}
    }
}
