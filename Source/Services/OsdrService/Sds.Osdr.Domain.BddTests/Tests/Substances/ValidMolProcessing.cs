using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Chemicals.Domain.Aggregates;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.ValueObjects;
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
    public class ValidMolProcessing : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }
        public ValidMolProcessing(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FileId = ProcessRecordsFile(JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", JohnId } }).Result;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidMol_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidMol_GenerateExpectedRecordsFileAggregate()
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
                Fields = new List<string>() { "StdInChI", "StdInChIKey", "SMILES" }
            }, options => options
                .ExcludingMissingMembers()
            );
            file.Images.Should().NotBeNullOrEmpty();
            file.Images.Should().HaveCount(1);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidMol_GenerateExpectedFileEntity()
        {
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;
            fileView.Should().EntityShouldBeEquivalentTo(file);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidMol_GenerateExpectedFileNode()
        {
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;
            fileNode.Should().NotBeNull();
            fileNode.Should().NodeShouldBeEquivalentTo(file);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public void ChemicalProcessing_ValidMol_GenerateOnlyOneRecord()
        {
            var recordIds = Fixture.GetProcessedRecords(FileId);
            recordIds.Should().HaveCount(1);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidMol_GenerateExpectedSubstanceAggregate()
        {
            var recordId = Fixture.GetProcessedRecords(FileId).First();

            var record = await Session.Get<Substance>(recordId);
            record.Should().NotBeNull();
            record.ShouldBeEquivalentTo(new
            {
                Id = recordId,
                RecordType = RecordType.Structure,
                Bucket = JohnId.ToString(),
                OwnedBy = JohnId,
                CreatedBy = JohnId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                UpdatedBy = JohnId,
                UpdatedDateTime = DateTimeOffset.UtcNow,
                ParentId = FileId,
                Status = RecordStatus.Processed,
                Index = 0,
                Issues = new List<Generic.Domain.ValueObjects.Issue>() { new Generic.Domain.ValueObjects.Issue { Code = "Code", AuxInfo = "AuxInfo", Message = "Message", Severity = Severity.Information, Title = "Title" } }
            }, options => options
                .ExcludingMissingMembers()
            );
			record.Images.Should().NotBeNullOrEmpty();
			record.Images.Should().ContainSingle();
			record.Fields.Should().NotBeNullOrEmpty();
			record.Fields.Should().HaveCount(3);
			record.Properties.Should().NotBeNullOrEmpty();
			record.Properties.Should().HaveCount(9);
            record.BlobId.Should().NotBeEmpty();
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidMol_GenerateExpectedRecordEntity()
        {
            var recordId = Fixture.GetProcessedRecords(FileId).First();
            var record = await Session.Get<Substance>(recordId);

            var recordView = Records.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
            recordView.Should().EntityShouldBeEquivalentTo(record);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task ChemicalProcessing_ValidMol_GenerateExpectedRecordNode()
        {
            var recordId = Fixture.GetProcessedRecords(FileId).First();
            var record = await Session.Get<Substance>(recordId);

            var recordNode = Nodes.Find(new BsonDocument("_id", recordId)).FirstOrDefault() as IDictionary<string, object>;
            recordNode.Should().NotBeNull();
            recordNode.Should().NodeShouldBeEquivalentTo(record);
        }
    }
}
