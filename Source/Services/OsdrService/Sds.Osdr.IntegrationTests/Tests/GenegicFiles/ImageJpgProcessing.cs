using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class ImageJpgProcessingFixture
    {
        public Guid FileId { get; set; }

        public ImageJpgProcessingFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessFile(harness.JohnId.ToString(), "computer-humor-computer-science.jpg", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class ImageJpgProcessing : OsdrTest, IClassFixture<ImageJpgProcessingFixture>
    {
        private Guid FileId { get; set; }
        private Guid BlobId => GetBlobId(FileId);
        
        public ImageJpgProcessing(OsdrTestHarness fixture, ITestOutputHelper output, ImageJpgProcessingFixture initFixture) : base(fixture, output)
        {
            output.WriteLine("image processing started");
            FileId = initFixture.FileId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Image)]
        public async Task ImageProcessing_ValidJpg_GeneratesAppropriateModels()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var file = await Session.Get<File>(FileId);
            file.Should().NotBeNull();
            file.ShouldBeEquivalentTo(new
                {
                    Id = FileId,
                    Type = FileType.Image,
                    Bucket = JohnId.ToString(),
                    BlobId = BlobId,
                    PdfBucket = file.Bucket,
                    OwnedBy = JohnId,
                    CreatedBy = JohnId,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    UpdatedBy = JohnId,
                    UpdatedDateTime = DateTimeOffset.UtcNow,
                    ParentId = JohnId,
                    FileName = blobInfo.FileName,
                    Length = blobInfo.Length,
                    Md5 = blobInfo.MD5,
                    IsDeleted = false,
                    Status = FileStatus.Processed
                }, options => options
                    .ExcludingMissingMembers()
            );
            file.Images.Should().NotBeNullOrEmpty();
            file.Images.Should().HaveCount(3);
        }
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Image)]
        public async Task ImageProcessing_ValidJpg_ExpectedFileEntity()
        {
            var file = await Session.Get<File>(FileId);
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileView.Should().EntityShouldBeEquivalentTo(file);
        }
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Image)]
        public async Task ImageProcessing_ValidJpg_ExpectedFileNode()
        {
            var file = await Session.Get<File>(FileId);
            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileNode.Should().NodeShouldBeEquivalentTo(file);
        }
    }
}