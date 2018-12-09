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
    public class InvalidJdxProcessingFixture
    {
        public Guid FileId { get; set; }

        public InvalidJdxProcessingFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "13Csample.jdx", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class InvalidJdxProcessing : OsdrTest, IClassFixture<InvalidJdxProcessingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public InvalidJdxProcessing(OsdrTestHarness fixture, ITestOutputHelper output, InvalidJdxProcessingFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Spectrum)]
        public async Task SpectrumProcessing_InvalidJdx_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Spectrum)]
		public async Task SpectrumProcessing_InvalidJdx_GeneratesExpectedFileAggregate()
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

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Spectrum)]
		public async Task SpectrumProcessing_InvalidJdx_GeneratesExpectedFileEntity()
		{
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileView.Should().EntityShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Spectrum)]
		public async Task SpectrumProcessing_InvalidJdx_GeneratesExpectedFileNode()
		{
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

			fileNode.Should().NotBeNull();
			fileNode.Should().NodeShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Spectrum)]
		public async Task SpectrumProcessing_InvalidJdx_GeneratesExpectedRecordNode()
		{
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var recordNode = Nodes.Find(new BsonDocument("FileId", FileId)).FirstOrDefault() as IDictionary<string, object>;

            recordNode.Should().BeNull();
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Spectrum)]
		public async Task SpectrumProcessing_InvalidJdx_GeneratesExpectedRecordEntity()
		{
			var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);
            var recordViews = Records.Find(new BsonDocument("FileId", FileId)).FirstOrDefault() as IDictionary<string, object>;

            recordViews.Should().BeNull();
		}
    }
}
