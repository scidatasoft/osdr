using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Office.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class MsWordProcessing : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public MsWordProcessing(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FileId = ProcessFile(JohnId.ToString(), "Developing Standard Approaches for Curating Small Molecule Pharmaceuticals_Jan18_2013.doc", new Dictionary<string, object>() { { "parentId", JohnId } }).Result;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Office)]
        public async Task OfficeProcessing_ValidMsWord_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Office)]
        public async Task OfficeProcessing_ValidMsWord_GeneratesAppropriateModels()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();
			
            var file = await Session.Get<OfficeFile>(FileId);
			file.Should().NotBeNull();
			file.Should().ShouldBeEquivalentTo(new {
                Id = FileId,
                Type = FileType.Office,
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

			file.PdfBlobId.Should().NotBeEmpty();
			file.PdfBlobId.Should().NotBe(file.BlobId);
        }

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Office)]
		public async Task OfficeProcessing_ValidMsWord_ExpectedFileEntity()
		{
            var file = await Session.Get<OfficeFile>(FileId);
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

			fileView.Should().NotBeNull();
			fileView.Should().EntityShouldBeEquivalentTo(file);
		}
		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Office)]
		public async Task OfficeProcessing_ValidMsWord_ExpectedFileNode()
		{
            var file = await Session.Get<OfficeFile>(FileId);
            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

			fileNode.Should().NotBeNull();
			fileNode.Should().OfficeNodeShouldBeEquivalentTo(file);
		}
    }
}
