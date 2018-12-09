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
    public class CreatePublicLinkForFileFixture
    {
        public Guid FileId { get; set; }

        public CreatePublicLinkForFileFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            var file = harness.Session.Get<RecordsFile.Domain.RecordsFile>(FileId).Result;
            var response = harness.JohnApi.SetPublicFileEntity(FileId, file.Version, true).Result;
            harness.WaitWhileFileShared(FileId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class CreatePublicLinkForFile : OsdrWebTest, IClassFixture<CreatePublicLinkForFileFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public CreatePublicLinkForFile(OsdrWebTestHarness fixture, ITestOutputHelper output, CreatePublicLinkForFileFixture initFixture) 
            : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithAuthorizeUser_ReturnsExpectedBlobFile()
        {
            var blobResponse = await JohnApi.GetBlobFileEntityById(FileId, BlobId);
            blobResponse.EnsureSuccessStatusCode();
            blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
            blobResponse.Content.Headers.ContentType.MediaType.ShouldBeEquivalentTo("application/octet-stream");
            blobResponse.Content.Headers.ContentLength.ShouldBeEquivalentTo(1689);
            //blobResponse.Content.Headers.ContentDisposition.FileName.ShouldBeEquivalentTo("Aspirin.mol");
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithAuthorizeUser2_ReturnsExpectedBlobFile()
        {
            var blobResponse = await JaneApi.GetBlobFileEntityById(FileId, BlobId);
            blobResponse.EnsureSuccessStatusCode();
            blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
            blobResponse.Content.Headers.ContentType.MediaType.ShouldBeEquivalentTo("application/octet-stream");
            blobResponse.Content.Headers.ContentLength.ShouldBeEquivalentTo(1689);
            //blobResponse.Content.Headers.ContentDisposition.FileName.ShouldBeEquivalentTo("Aspirin.mol");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithAuthorizeUser_ReturnsExpectedBlobFileAndRecord()
        {
            var nodeRecordResponse = await JohnApi.GetNodesById(FileId);
            var nodeRecord = JArray.Parse(await nodeRecordResponse.Content.ReadAsStringAsync()).First();

            var nodeRecordId = nodeRecord["id"].ToObject<Guid>();
            var recordResponse = await JohnApi.GetRecordEntityById(nodeRecordId);
            var record = JObject.Parse(await recordResponse.Content.ReadAsStringAsync());
            var recordId = record["id"].ToObject<Guid>();
            var recordBlobId = record["blob"]["id"].ToObject<Guid>();
            var blobRecordResponse = await JohnApi.GetBlobRecordEntityById(recordId, recordBlobId);
            var recordString = await blobRecordResponse.Content.ReadAsStringAsync();
            var blobFileResponse = await UnauthorizedApi.GetBlobFileEntityById(FileId, BlobId);
            var fileString = await blobFileResponse.Content.ReadAsStringAsync();

            recordString.Should().BeEquivalentTo(fileString);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedBlobFile()
        {
            var blobResponse = await UnauthorizedApi.GetBlobFileEntityById(FileId, BlobId);
            blobResponse.EnsureSuccessStatusCode();
            blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
            blobResponse.Content.Headers.ContentType.MediaType.ShouldBeEquivalentTo("application/octet-stream");
            blobResponse.Content.Headers.ContentLength.ShouldBeEquivalentTo(1689);
            //blobResponse.Content.Headers.ContentDisposition.FileName.ShouldBeEquivalentTo("Aspirin.mol");
        }
	    
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithAuthorizeUser_ReturnsRecordNotFound()
        {
            var blobResponse = await JohnApi.GetBlobRecordEntityById(FileId, BlobId);
            blobResponse.IsSuccessStatusCode.ShouldBeEquivalentTo(false);
            blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
            blobResponse.ReasonPhrase.ShouldAllBeEquivalentTo("Not Found");
        }
    }
}