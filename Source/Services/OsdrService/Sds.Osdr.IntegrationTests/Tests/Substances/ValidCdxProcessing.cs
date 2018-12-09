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
    public class ValidCdxProcessingFixture
    {
        public Guid FileId { get; set; }

        public ValidCdxProcessingFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "125_11Mos.cdx", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class ValidCdxProcessing : OsdrTest, IClassFixture<ValidCdxProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public ValidCdxProcessing(OsdrTestHarness fixture, ITestOutputHelper output, ValidCdxProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidCdx_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
		public async Task ChemicalProcessing_ValidCdx_GenerateExpectedRecordsFileAggregate()
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
				TotalRecords = 3,
				Fields = new List<string>()
			}, options => options
				.ExcludingMissingMembers()
			);
			file.Images.Should().NotBeNullOrEmpty();
			file.Images.Count.Should().Be(1);
		}

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidCdx_GenerateExpectedFileEntity()
        {
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileView.Should().EntityShouldBeEquivalentTo(file);
        }
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidCdx_GenerateExpectedFileNode()
        {
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

			fileNode.Should().NotBeNull();
			fileNode.Should().NodeShouldBeEquivalentTo(file);
        }
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidCdxWithThreeRecords_GenerateExpectedRecordsEntityAndRecordsNode()
        {
            var records = Harness.GetProcessedRecords(FileId);
            records.Should().HaveCount(3);

            foreach (var recordId in records)
            {
                var recordView = Records.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
                recordView.Should().NotBeNull();

                var recordBlob = recordView["Blob"];
				recordBlob.Should().NotBeNull();
				recordBlob.Should().BeAssignableTo<IDictionary<string, object>>();

                var recordBlobId = (recordBlob as IDictionary<string, object>)["_id"];
				recordBlobId.Should().NotBeNull();
				recordBlobId.Should().BeOfType<Guid>();

                var index = Convert.ToInt32(recordView["Index"]);
                index.Should().BeGreaterOrEqualTo(0);

                var record = await Session.Get<Substance>((Guid)recordId);
                record.Should().NotBeNull();
                record.ShouldBeEquivalentTo(new
                {
                    Id = recordId,
                    RecordType = RecordType.Structure,
                    Bucket = JohnId.ToString(),
                    BlobId = recordBlobId,
                    OwnedBy = JohnId,
                    CreatedBy = JohnId,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    UpdatedBy = JohnId,
                    UpdatedDateTime = DateTimeOffset.UtcNow,
                    ParentId = FileId,
                    Status = RecordStatus.Processed,
                    Index = index,
                    //Issues = new List<Generic.Domain.ValueObjects.Issue>() { new Generic.Domain.ValueObjects.Issue { Code = "Code", AuxInfo = "AuxInfo", Message = "Message", Severity = Generic.Domain.ValueObjects.Severity.Information, Title = "Title" } }
                    Issues = new List<Generic.Domain.ValueObjects.Issue>() { }
                }, options => options
                    .ExcludingMissingMembers()
                );
				record.Images.Should().NotBeNullOrEmpty();
				record.Images.Should().ContainSingle();
                record.Fields.Should().BeEmpty();
				record.Properties.Should().NotBeNullOrEmpty();
				record.Properties.Should().HaveCount(9);

                recordView.Should().EntityShouldBeEquivalentTo(record);

                var recordNode = Nodes.Find(new BsonDocument("_id", (Guid)recordId)).FirstOrDefault() as IDictionary<string, object>;
				recordNode.Should().NotBeNull();
				recordNode.Should().NodeShouldBeEquivalentTo(record);
            }
        }
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public void ChemicalProcessing_ValidCdx_GenerateOnlyThreeRecord()
        {
            var recordIds = Harness.GetProcessedRecords(FileId);
            recordIds.Should().HaveCount(3);
        }
    }
}
