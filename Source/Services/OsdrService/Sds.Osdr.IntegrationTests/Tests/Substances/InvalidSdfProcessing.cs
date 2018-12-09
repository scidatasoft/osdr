using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Chemicals.Domain.Aggregates;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.RecordsFile.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class InvalidSdfProcessingFixture
    {
        public Guid FileId { get; set; }

        public InvalidSdfProcessingFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "invalid_sdf_with_20_records_where_first_and_second_are_invalid.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class InvalidSdfProcessing : OsdrTest, IClassFixture<InvalidSdfProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public InvalidSdfProcessing(OsdrTestHarness fixture, ITestOutputHelper output, InvalidSdfProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidSdf_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_InvalidSdf_GenerateExceptedFileAggregate()
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
				TotalRecords = 20,
				Fields = new List<string>() { "StdInChI", "StdInChIKey", "SMILES" }
			}, options => options
				.ExcludingMissingMembers()
			);
			file.Images.Count.Should().Be(1);
		}

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Crystal)]
        public void ChemicalProcessing_InvalidSdf_GenerateOnlyTwoInvalidRecord()
        {
            var recordIds = Harness.GetInvalidRecords(FileId);
            recordIds.Should().HaveCount(2);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Crystal)]
        public void ChemicalProcessing_InvalidSdf_Generate18ValidRecord()
        {
            var recordIds = Harness.GetProcessedRecords(FileId);
            recordIds.Should().HaveCount(18);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidSdfWithTwentyRecords_GenerateExpectedInvalidRecordAggregate()
        {
            var recordId = Harness.GetInvalidRecords(FileId).First();

            var invalidRecord = await Session.Get<InvalidRecord>(recordId);
            invalidRecord.Should().NotBeNull();

            invalidRecord.ShouldBeEquivalentTo(new
            {
                Id = recordId,
                Error = "sdffile loader: could not process file",
                RecordType = RecordType.Structure,
                OwnedBy = Harness.JohnId,
                CreatedBy = Harness.JohnId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                UpdatedBy = Harness.JohnId,
                UpdatedDateTime = DateTimeOffset.UtcNow,
                ParentId = FileId,
                Status = RecordStatus.Failed,
                //Index = 0,
            }, options => options
                .ExcludingMissingMembers()
            );
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidSdfWithTwentyRecords_GenerateExpectedRecordEntity()
        {
            var recordId = Harness.GetInvalidRecords(FileId).First();

            var invalidRecord = await Session.Get<InvalidRecord>(recordId);
            invalidRecord.Should().NotBeNull();

            var recordEntity = Records.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
            recordEntity.Should().NotBeNull().And.EntityShouldBeEquivalentTo(invalidRecord);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidSdfWithTwentyRecords_GenerateExpectedRecordNode()
        {
            var recordId = Harness.GetInvalidRecords(FileId).First();

            var invalidRecord = await Session.Get<InvalidRecord>(recordId);
            invalidRecord.Should().NotBeNull();

            var recordNode = Nodes.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
            recordNode.Should().NotBeNull().And.NodeShouldBeEquivalentTo(invalidRecord);
        }
    }
}
