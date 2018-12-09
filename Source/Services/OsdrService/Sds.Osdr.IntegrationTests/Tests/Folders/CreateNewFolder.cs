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
    public class NewFolderFixture
    {
        public Guid FolderId { get; }

        public NewFolderFixture(OsdrTestHarness harness)
        {
            FolderId = harness.CreateFolder("new folder", harness.JohnId, harness.JohnId).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class CreateNewFolder : OsdrTest, IClassFixture<NewFolderFixture>
    {
        private Guid FolderId { get; }

        public CreateNewFolder(OsdrTestHarness fixture, ITestOutputHelper output, NewFolderFixture initFixture) : base(fixture, output)
        {
            FolderId = initFixture.FolderId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Folder)]
        public async Task CreateFlder_NewFolder_RegisterNewFolder()
        {
            var folder = await Session.Get<Folder>(FolderId);

			folder.Should().NotBeNull();
			folder.Should().ShouldBeEquivalentTo(new {
                Id = FolderId,
                OwnedBy = JohnId,
                CreatedBy= JohnId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                UpdatedBy = JohnId,
                UpdatedDateTime = DateTimeOffset.UtcNow,
                ParentId = JohnId,
                Version = 1,
                Name = "new folder",
                Status = FolderStatus.Created,
                IsDeleted = false
            }, options => options
                .ExcludingMissingMembers()
            );
        }

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Folder)]
		public async Task CreateFlder_NewFolder_ExpectedFolderEntity()
		{
			var folder = await Session.Get<Folder>(FolderId);
            var folderView = Folders.Find(new BsonDocument("_id", FolderId)).FirstOrDefault() as IDictionary<string, object>;

            folderView.Should().EntityShouldBeEquivalentTo(folder);
		}

		[Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Folder)]
		public async Task CreateFlder_NewFolder_ExpectedFolderNode()
		{
			var folder = await Session.Get<Folder>(FolderId);
            var folderNode = Nodes.Find(new BsonDocument("_id", FolderId)).FirstOrDefault() as IDictionary<string, object>;

            folderNode.Should().NodeShouldBeEquivalentTo(folder);
		}
    }
}
