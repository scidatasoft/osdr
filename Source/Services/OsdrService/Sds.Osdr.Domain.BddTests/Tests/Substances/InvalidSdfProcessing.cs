using FluentAssertions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests;
using Sds.Osdr.Generic.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class InvalidSdfProcessing : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public InvalidSdfProcessing(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FileId = ProcessRecordsFile(JohnId.ToString(), "invalid_sdf_with_20_records_where_first_and_second_are_invalid.sdf", new Dictionary<string, object>() { { "parentId", JohnId } }).Result;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidSdf_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

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
        public void ChemicalProcessing_InvalidSdf_GenerateOnlyTwoRecord()
        {
            var recordIds = Fixture.GetInvalidRecords(FileId);
            recordIds.Should().HaveCount(2);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_InvalidSdfWithTwentyRecordsWhereFirstAndSecondAreInvalid_GenerateTwoInvalidRecords()
        {
            //         var fileNode = Nodes.Find(new BsonDocument("_id", (Guid)fileId)).FirstOrDefault() as IDictionary<string, object>;
            //         fileNode.Should().NotBeNull().And.NodeShouldBeEquivalentTo(file);

            //         var recordViews = Records.Find(new BsonDocument("FileId", (Guid)fileId)).ToList();
            //recordViews.Should().NotBeNullOrEmpty().And.HaveCount(2);

            //         var invalidRecordView = recordViews.Single(r => r.Index == 0) as IDictionary<string, object>;
            //         invalidRecordView.Should().NotBeNull();

            //         var recordId = invalidRecordView["_id"];

            //         var invalidRecord = await Session.Get<InvalidRecord>((Guid)recordId);
            //         invalidRecord.Should().NotBeNull();

            //         invalidRecord.ShouldBeEquivalentTo(new
            //         {
            //             Id = recordId,
            //             Error = "sdffile loader: could not process file",
            //             RecordType = RecordType.Structure,
            //             OwnedBy = UserId,
            //             CreatedBy = UserId,
            //             CreatedDateTime = startTime,
            //             UpdatedBy = UserId,
            //             UpdatedDateTime = startTime,
            //             ParentId = fileId,
            //             Status = RecordStatus.Failed,
            //             Index = 0,
            //         }, options => options
            //             .ExcludingMissingMembers()
            //         );

            //         var recordEntity = Records.Find(new BsonDocument("_id", (Guid)recordId)).FirstOrDefault() as IDictionary<string, object>;
            //         recordEntity.Should().NotBeNull().And.EntityShouldBeEquivalentTo(invalidRecord);

            //         var recordNode = Nodes.Find(new BsonDocument("_id", (Guid)recordId)).FirstOrDefault() as IDictionary<string, object>;
            //         recordNode.Should().NotBeNull().And.NodeShouldBeEquivalentTo(invalidRecord);

            //         var validRecordView = recordViews.Single(r => r.Index == 1) as IDictionary<string, object>;
            //         validRecordView.Should().NotBeNull();

            //         recordId = validRecordView["_id"];

            //         var validRecord = await Session.Get<Substance>((Guid)recordId);
            //         validRecord.Should().NotBeNull();

            //         validRecord.ShouldBeEquivalentTo(new
            //         {
            //             Id = recordId,
            //             RecordType = RecordType.Structure,
            //             OwnedBy = UserId,
            //             CreatedBy = UserId,
            //             CreatedDateTime = startTime,
            //             UpdatedBy = UserId,
            //             UpdatedDateTime = startTime,
            //             ParentId = fileId,
            //             Status = RecordStatus.Processed,
            //             Index = 1,
            //         }, options => options
            //             .ExcludingMissingMembers()
            //         );

            //         recordEntity = Records.Find(new BsonDocument("_id", (Guid)recordId)).FirstOrDefault() as IDictionary<string, object>;
            //         recordEntity.Should().NotBeNull().And.EntityShouldBeEquivalentTo(validRecord);

            //         recordNode = Nodes.Find(new BsonDocument("_id", (Guid)recordId)).FirstOrDefault() as IDictionary<string, object>;
            //         recordNode.Should().NotBeNull().And.NodeShouldBeEquivalentTo(validRecord);
        }
    }
}
