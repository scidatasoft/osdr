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

namespace Sds.Osdr.WebApi.IntegrationTests.Tests.Blobs
{
    public class CreatePublicLinkForRecordFixture
    {
        public Guid FileId { get; set; }

        public CreatePublicLinkForRecordFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            var file = harness.Session.Get<RecordsFile.Domain.RecordsFile>(FileId).Result;
            var response = harness.JohnApi.SetPublicFileEntity(FileId, file.Version, true).Result;
            harness.WaitWhileFileShared(FileId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class CreatePublicLinkForRecord : OsdrWebTest, IClassFixture<CreatePublicLinkForRecordFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public CreatePublicLinkForRecord(OsdrWebTestHarness fixture, ITestOutputHelper output, CreatePublicLinkForRecordFixture initFixture) 
            : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedBlobRecord()
        {
            var nodeRecordResponse = await JohnApi.GetNodesById(FileId);
            var nodeRecord = JArray.Parse(await nodeRecordResponse.Content.ReadAsStringAsync()).First();

            var nodeRecordId = nodeRecord["id"].ToObject<Guid>();
            var recordResponse = await JohnApi.GetRecordEntityById(nodeRecordId);
            var record = JObject.Parse(await recordResponse.Content.ReadAsStringAsync());
            var recordId = record["id"].ToObject<Guid>();
            var recordBlobId = record["blob"]["id"].ToObject<Guid>();

            var blobResponse = await UnauthorizedApi.GetBlobRecordEntityById(recordId, recordBlobId);
            blobResponse.EnsureSuccessStatusCode();
            blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
            blobResponse.Content.Headers.ContentType.MediaType.ShouldBeEquivalentTo("chemical/x-mdl-molfile");
            blobResponse.Content.Headers.ContentLength.ShouldBeEquivalentTo(1689);
            //blobResponse.Content.Headers.ContentDisposition.FileName.Should().NotBeNullOrEmpty();
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharin_WithAuthorizeUser_ReturnsExpectedBlobRecord()
        {
            var nodeRecordResponse = await JohnApi.GetNodesById(FileId);
            var nodeRecord = JArray.Parse(await nodeRecordResponse.Content.ReadAsStringAsync()).First();

            var nodeRecordId = nodeRecord["id"].ToObject<Guid>();
            var recordResponse = await JohnApi.GetRecordEntityById(nodeRecordId);
            var record = JObject.Parse(await recordResponse.Content.ReadAsStringAsync());
            var recordId = record["id"].ToObject<Guid>();
            var recordBlobId = record["blob"]["id"].ToObject<Guid>();

            var blobResponse = await JohnApi.GetBlobRecordEntityById(recordId, recordBlobId);
            blobResponse.EnsureSuccessStatusCode();
            blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
            blobResponse.Content.Headers.ContentType.MediaType.ShouldBeEquivalentTo("chemical/x-mdl-molfile");
            blobResponse.Content.Headers.ContentLength.ShouldBeEquivalentTo(1689);
            //blobResponse.Content.Headers.ContentDisposition.FileName.Should().NotBeNullOrEmpty();
        }
    }
}