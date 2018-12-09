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
    public class SharingPngFileFixture
    {
        public Guid FileId { get; set; }

        public SharingPngFileFixture(OsdrWebTestHarness harness)
        {
            FileId = harness.ProcessFile(harness.JohnId.ToString(), "Chemical-diagram.png", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;

            var file = harness.Session.Get<RecordsFile.Domain.RecordsFile>(FileId).Result;
            var response = harness.JohnApi.SetPublicFileEntity(FileId, file.Version, true).Result;
            harness.WaitWhileFileShared(FileId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class SharingPngFile : OsdrWebTest, IClassFixture<SharingPngFileFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public SharingPngFile(OsdrWebTestHarness fixture, ITestOutputHelper output, SharingPngFileFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithAuthorizeUser_ReturnsExpectedImage()
        {
            var fileResponse = await JohnApi.GetFileEntityById(FileId);
            var file = JObject.Parse(await fileResponse.Content.ReadAsStringAsync());
            var imageId = file["images"].First()["id"].ToObject<Guid>();

            var blobResponse = await JohnApi.GetImagesFileEntityById(FileId, imageId);
            blobResponse.EnsureSuccessStatusCode();
            blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
            blobResponse.Content.Headers.ContentType.MediaType.ShouldBeEquivalentTo("application/octet-stream");
            blobResponse.Content.Headers.ContentLength.ShouldBeEquivalentTo(175430);
            blobResponse.Content.Headers.ContentDisposition.FileName.ShouldBeEquivalentTo("Chemical-diagram.png");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnauthorizeUser_ReturnsExpectedImage()
        {
            var fileResponse = await JohnApi.GetFileEntityById(FileId);
            var file = JObject.Parse(await fileResponse.Content.ReadAsStringAsync());
            var imageId = file["images"].First()["id"].ToObject<Guid>();

            var blobResponse = await UnauthorizedApi.GetImagesFileEntityById(FileId, imageId);
            blobResponse.EnsureSuccessStatusCode();
            blobResponse.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
            blobResponse.Content.Headers.ContentType.MediaType.ShouldBeEquivalentTo("application/octet-stream");
            blobResponse.Content.Headers.ContentLength.ShouldBeEquivalentTo(175430);
            blobResponse.Content.Headers.ContentDisposition.FileName.ShouldBeEquivalentTo("Chemical-diagram.png");
        }
    }
}