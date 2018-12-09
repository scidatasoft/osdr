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
    public class ArchiveGzProcessingFixture
    {
        public Guid FileId { get; set; }

        public ArchiveGzProcessingFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessFile(harness.JohnId.ToString(), "IMG_0109.gz", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class ArchiveGzProcessing : OsdrTest, IClassFixture<ArchiveGzProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public ArchiveGzProcessing(OsdrTestHarness fixture, ITestOutputHelper output, ArchiveGzProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Generic)]
        public async Task GenericProcessing_ValidGz_GeneratesAppropriateModels()
        {
            var startTime = DateTimeOffset.UtcNow;

            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var file = await Session.Get<File>(FileId);
            file.Should().NotBeNull();
			file.ShouldBeEquivalentTo(new
			{
				Id = FileId,
				Type = FileType.Generic,
				Bucket = JohnId.ToString(),
				BlobId = BlobId,
				OwnedBy = JohnId,
				CreatedBy = JohnId,
				CreatedDateTime = startTime,
				UpdatedBy = JohnId,
				UpdatedDateTime = startTime,
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

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Generic)]
		public async Task GenericProcessing_ValidGz_ExpectedFileEntity()
		{
			var file = await Session.Get<File>(FileId);
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileView.Should().EntityShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Generic)]
		public async Task GenericProcessing_ValidGz_ExpectedFileNode()
		{
			var file = await Session.Get<File>(FileId);
            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileNode.Should().NodeShouldBeEquivalentTo(file);
		}
    }
}
