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
    public class SdfProcessingWithOneValidAndOneInvalidRecordsFixture
    {
        public Guid FileId { get; set; }

        public SdfProcessingWithOneValidAndOneInvalidRecordsFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "test_solubility.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class SdfProcessingWithOneValidAndOneInvalidRecords : OsdrTest, IClassFixture<SdfProcessingWithOneValidAndOneInvalidRecordsFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public SdfProcessingWithOneValidAndOneInvalidRecords(OsdrTestHarness fixture, ITestOutputHelper output, SdfProcessingWithOneValidAndOneInvalidRecordsFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_GenerateExpectedFileAggregate()
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
				TotalRecords = 2,
				Fields = new List<string>() { "StdInChI", "StdInChIKey", "SMILES" }
			}, options => options
				.ExcludingMissingMembers()
			);
			file.Images.Count.Should().Be(1);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_GenerateExpectedFileEntity()
		{
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
			var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

			fileView.Should().EntityShouldBeEquivalentTo(file);
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_GenerateExpectedFileNode()
		{
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
			var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

			fileNode.Should().NotBeNull();
			fileNode.Should().NodeShouldBeEquivalentTo(file);
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_GenerateExpectedOnlyTwoValidRecords()
		{
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
			var recordViews = Records.Find(new BsonDocument("FileId", FileId)).ToList();

			recordViews.Should().NotBeNullOrEmpty();
			recordViews.Should().HaveCount(2);
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_GenerateExpectedInvalidRecord()
		{
            var recordId = Harness.GetInvalidRecords(FileId).First();

            var invalidRecord = await Session.Get<InvalidRecord>(recordId);
            invalidRecord.Should().NotBeNull();

            invalidRecord.ShouldBeEquivalentTo(new
            {
                Id = recordId,
                Error = "sdffile loader: could not process file",
                RecordType = RecordType.Structure,
                OwnedBy = JohnId,
                CreatedBy = JohnId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                UpdatedBy = JohnId,
                UpdatedDateTime = DateTimeOffset.UtcNow,
                ParentId = FileId,
                Status = RecordStatus.Failed,
                Index = 0,
            }, options => options
                .ExcludingMissingMembers()
            );
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_GenerateExpectedInvalidRecordEntity()
		{
            var recordId = Harness.GetInvalidRecords(FileId).First();
            var invalidRecord = await Session.Get<InvalidRecord>(recordId);
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

            var recordEntity = Records.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
			recordEntity.Should().NotBeNull();
			recordEntity.Should().EntityShouldBeEquivalentTo(invalidRecord);
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_GenerateExpectedRecordNode()
		{
            var recordId = Harness.GetInvalidRecords(FileId).First();
            var invalidRecord = await Session.Get<InvalidRecord>(recordId);

            var recordNode = Nodes.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
			recordNode.Should().NotBeNull();
			recordNode.Should().NodeShouldBeEquivalentTo(invalidRecord);
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_GenerateExpectedValidRecordSubstanceAggregate()
		{
            var recordId = Harness.GetProcessedRecords(FileId).First();

            var validRecord = await Session.Get<Substance>(recordId);
            validRecord.Should().NotBeNull();

            validRecord.ShouldBeEquivalentTo(new
            {
                Id = recordId,
                RecordType = RecordType.Structure,
                OwnedBy = JohnId,
                CreatedBy = JohnId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                UpdatedBy = JohnId,
                UpdatedDateTime = DateTimeOffset.UtcNow,
                ParentId = FileId,
                Status = RecordStatus.Processed,
                Index = 1,
            }, options => options
                .ExcludingMissingMembers()
            );
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_OneValidSdfAndOneInvalid_GenerateExpectedRecordEntity()
		{
            var recordId = Harness.GetProcessedRecords(FileId).First();

            var validRecord = await Session.Get<Substance>(recordId);
            var recordEntity = Records.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
			recordEntity.Should().NotBeNull();
			recordEntity.Should().EntityShouldBeEquivalentTo(validRecord);
		}
		
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public void ChemicalProcessing_ValidSdf_GenerateOnlyOneRecord()
        {
            var recordIds = Harness.GetInvalidRecords(FileId);
            recordIds.Should().HaveCount(1);
        }
    }
}
