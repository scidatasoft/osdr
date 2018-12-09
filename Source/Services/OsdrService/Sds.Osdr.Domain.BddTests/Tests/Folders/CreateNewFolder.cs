using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests.Extensions;
using Sds.Osdr.Generic.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class CreateNewFolder : OsdrTest
    {
        private Guid FolderId { get; }

        public CreateNewFolder(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FolderId = Harness.CreateFolder("new folder", JohnId, JohnId).Result;
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
