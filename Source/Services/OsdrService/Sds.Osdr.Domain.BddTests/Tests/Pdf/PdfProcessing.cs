using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Pdf.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Sds.Osdr.Domain.BddTests;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class PdfProcessing : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public PdfProcessing(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FileId = ProcessFile(JohnId.ToString(), "Abdelaziz A Full_manuscript.pdf", new Dictionary<string, object>() { { "parentId", JohnId } }).Result;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Pdf)]
        public async Task PdfProcessing_ValidPdf_GeneratesAppropriateModels()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var file = await Session.Get<PdfFile>(FileId);
			file.Should().NotBeNull();
			file.Should().ShouldBeEquivalentTo(new
            {
                Id = FileId,
                Type = FileType.Pdf,
                Bucket = JohnId.ToString(),
                BlobId = BlobId,
                PdfBucket = file.Bucket,
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
                Status = FileStatus.Processed
            }, options => options
                .ExcludingMissingMembers()
            );
        }

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Pdf)]
		public async Task PdfProcessing_ValidPdf_ExpectedFileEntity()
		{
            var file = await Session.Get<PdfFile>(FileId);
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileView.Should().EntityShouldBeEquivalentTo(file);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Pdf)]
		public async Task PdfProcessing_ValidPdf_ExpectedFileNode()
		{
            var file = await Session.Get<PdfFile>(FileId);
            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileNode.Should().NodeShouldBeEquivalentTo(file);
		}
    }
}
