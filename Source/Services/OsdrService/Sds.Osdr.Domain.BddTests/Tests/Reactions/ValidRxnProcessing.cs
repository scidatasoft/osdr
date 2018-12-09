using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Domain;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Reactions.Domain;
using Sds.Osdr.RecordsFile.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class ValidRxnProcessing : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

		public ValidRxnProcessing(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FileId = ProcessRecordsFile(JohnId.ToString(), "10001.rxn", new Dictionary<string, object>() { { "parentId", JohnId } }).Result;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_ValidRxn_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public async Task ReactionProcessing_ValidRxn_GenerateExpectedFileAggregate()
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
				Status = FileStatus.Processed,
				TotalRecords = 1,
				Fields = new string[] { "Field1", "Field2" }
			}, options => options
				.ExcludingMissingMembers()
			);
			file.Images.Should().NotBeNullOrEmpty();
			file.Images.Count.Should().Be(1);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public async Task ReactionProcessing_ValidRxn_GenerateExpectedFileEntity()
		{
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileView.Should().EntityShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public async Task ReactionProcessing_ValidRxn_GenerateExpectedFileNode()
		{
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

			fileNode.Should().NotBeNull();
			fileNode.Should().NodeShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public async Task ReactionProcessing_ValidRxn_GenerateExpectedRecordEntity()
		{
            var recordView = Records.Find(new BsonDocument("FileId", FileId)).FirstOrDefault() as IDictionary<string, object>;
            recordView.Should().NotBeNull();

			var recordId = Fixture.GetProcessedRecords(FileId).First();
            var record = await Session.Get<Reaction>(recordId);
            recordView.Should().EntityShouldBeEquivalentTo(record);
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public void ReactionProcessing_ValidRxn_GenerateExpectedRactionOneProcessedRecord()
		{
			var recordIds = Fixture.GetProcessedRecords(FileId);
			recordIds.Should().HaveCount(1);
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
		public async Task ReactionProcessing_ValidRxn_GenerateExpectedRactionAggregate()
		{
			var recordId = Fixture.GetProcessedRecords(FileId).First();
            var record = await Session.Get<Reaction>(recordId);

            record.Should().NotBeNull();
            record.ShouldBeEquivalentTo(new
            {
                Id = recordId,
                RecordType = RecordType.Reaction,
                Bucket = JohnId.ToString(),
                OwnedBy = JohnId,
                CreatedBy = JohnId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                UpdatedBy = JohnId,
                UpdatedDateTime = DateTimeOffset.UtcNow,
                ParentId = FileId,
                Status = RecordStatus.Processed,
                Index = 0,
                Fields = new Field[] {
                    new Field("Field1", "Value1"),
                    new Field("Field2", "Value2")
                },
                Issues = new Issue[] { }
            }, options => options
                .ExcludingMissingMembers()
            );
			record.Images.Should().NotBeNullOrEmpty();
			record.Images.Should().HaveCount(1);
			record.Properties.Should().NotBeNull();
			record.Properties.Should().HaveCount(0);
		}

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Reaction)]
        public async Task ReactionProcessing_ValidRxn_GenerateExpectedRecordNode()
        {
			var recordId = Fixture.GetProcessedRecords(FileId).First();
            var record = await Session.Get<Reaction>(recordId);

            var recordNode = Nodes.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
			recordNode.Should().NotBeNull();
			recordNode.Should().NodeShouldBeEquivalentTo(record);
        }
    }
}
