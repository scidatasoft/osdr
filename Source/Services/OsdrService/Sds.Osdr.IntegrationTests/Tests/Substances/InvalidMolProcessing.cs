using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
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
    public class InvalidMolProcessingFixture
    {
        public Guid FileId { get; set; }

        public InvalidMolProcessingFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "ringcount_0.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class InvalidMolProcessing : OsdrTest, IClassFixture<InvalidMolProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public InvalidMolProcessing(OsdrTestHarness fixture, ITestOutputHelper output, InvalidMolProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidMol_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_InvalidMol_GenerateExpectedFileAggregate()
		{
            var startTime = DateTimeOffset.UtcNow;

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
				CreatedDateTime = startTime,
                UpdatedBy = JohnId,
				UpdatedDateTime = startTime,
                ParentId = JohnId,
                FileName = blobInfo.FileName,
                Length = blobInfo.Length,
                Md5 = blobInfo.MD5,
                IsDeleted = false,
                Status = FileStatus.Failed,
                TotalRecords = 1,
				ParsedRecords = 0,
				FailedRecords = 1,
                Fields = new List<string>() {  }
            }, options => options
                .ExcludingMissingMembers()
            );
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_InvalidMol_GenerateExpectedFileEntity()
		{
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

			var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;
			fileView.Should().EntityShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_InvalidMol_GenerateExpectedFileNode()
		{
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;
			fileNode.Should().NotBeNull();
			fileNode.Should().NodeShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public void ChemicalProcessing_InvalidMol_GenerateOnlyOneRecord()
		{
			var recordIds = Harness.GetInvalidRecords(FileId);
			recordIds.Should().HaveCount(1);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_InvalidMol_GenerateExpectedInvalidRecord()
		{
			var recordId = Harness.GetInvalidRecords(FileId).First();
			recordId.Should().NotBeEmpty();

            var invalidRecord = await Session.Get<InvalidRecord>(recordId);

            invalidRecord.Should().NotBeNull();
	    	invalidRecord.ShouldBeEquivalentTo(new
	    	{
	    		Id = recordId,
	    		Error = "molfile loader: ring bond count is allowed only for queries",
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
		public void ChemicalProcessing_InvalidMol_GenerateExpectedRecordEntity()
		{
			var recordId = Harness.GetInvalidRecords(FileId).First();
			recordId.Should().NotBeEmpty();

            var recordEntity = Records.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
            recordEntity.Should().NotBeNull();
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public void ChemicalProcessing_InvalidMol_GenerateExpectedInvalidRecordNode()
		{
			var recordId = Harness.GetInvalidRecords(FileId).First();
			recordId.Should().NotBeEmpty();

            var recordNode = Nodes.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
            recordNode.Should().NotBeNull();
		}
    }
}
