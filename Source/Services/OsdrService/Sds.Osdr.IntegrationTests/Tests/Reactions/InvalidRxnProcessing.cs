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
    public class InvalidRxnProcessingFixture
    {
        public Guid FileId { get; set; }

        public InvalidRxnProcessingFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "empty.rxn", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class InvalidRxnProcessing : OsdrTest, IClassFixture<InvalidRxnProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public InvalidRxnProcessing(OsdrTestHarness fixture, ITestOutputHelper output, InvalidRxnProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_InvalidRxn_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public async Task ReactionProcessing_InvalidRxn_GeneratesExpectedFileAggregate()
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
				.Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 5000)).WhenTypeIs<DateTimeOffset>()
			);
			file.Images.Should().BeEmpty();
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public async Task ReactionProcessing_InvalidRxn_GeneratesExpectedFileEntity()
		{
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileView.Should().EntityShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public async Task ReactionProcessing_InvalidRxn_GeneratesExpectedFileNode()
		{
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

			fileNode.Should().NotBeNull();
			fileNode.Should().NodeShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public void ReactionProcessing_InvalidRxn_GeneratesExpectedRecordEntity()
		{
            var recordViews = Records.Find(new BsonDocument("FileId", FileId)).FirstOrDefault() as IDictionary<string, object>;
            recordViews.Should().BeNull();
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public void ReactionProcessing_InvalidRxn_GeneratesExpectedRecordNode()
		{
            var recordNode = Nodes.Find(new BsonDocument("FileId", FileId)).FirstOrDefault() as IDictionary<string, object>;
            recordNode.Should().BeNull();
		}
    }
}
