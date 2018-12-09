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
    public class InvalidCifProcessingFixture
    {
        public Guid FileId { get; set; }

        public InvalidCifProcessingFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "1100110_modified.cif", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class InvalidCifProcessing : OsdrTest, IClassFixture<InvalidCifProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public InvalidCifProcessing(OsdrTestHarness fixture, ITestOutputHelper output, InvalidCifProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Crystal)]
        public async Task CrystalProcessing_InvalidCif_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Crystal)]
		public async Task CrystalProcessing_InvalidCif_GenerateExpectedFileAggregate()
		{
			var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
			blobInfo.Should().NotBeNull();

			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

			file.Should().NotBeNull();
			file.ShouldBeEquivalentTo(new
			{
				Id = FileId,
				Type = FileType.Records,
				Bucket = JohnId.ToString(),
				BlobId = BlobId,
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
				Status = FileStatus.Failed,
				TotalRecords = 0
			}, options => options
				.ExcludingMissingMembers()
			);
			file.Images.Should().BeEmpty();
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Crystal)]
		public async Task CrystalProcessing_InvalidCif_GenerateExpectedFileEntity()
		{
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

            fileView.Should().NotBeNull();
            fileView.Should().EntityShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Crystal)]
		public async Task CrystalProcessing_InvalidCif_GenerateExpectedFileNode()
		{
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;
			fileNode.Should().NotBeNull();
			fileNode.Should().NodeShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Crystal)]
		public void CrystalProcessing_InvalidCif_GenerateExpectedRecordEntity()
		{
            var recordViews = Records.Find(new BsonDocument("FileId", FileId)).FirstOrDefault() as IDictionary<string, object>;
            recordViews.Should().BeNull();
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Crystal)]
		public void CrystalProcessing_InvalidCif_GenerateExpectedRecordNode()
		{
            var recordNode = Nodes.Find(new BsonDocument("FileId", FileId)).FirstOrDefault() as IDictionary<string, object>;
            recordNode.Should().BeNull();
		}
    }
}
