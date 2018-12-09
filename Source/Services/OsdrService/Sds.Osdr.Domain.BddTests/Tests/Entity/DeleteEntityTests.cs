using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Domain.BddTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sds.Osdr.BddTests.FluentAssersions;

namespace Sds.Osdr.BddTests
{
    public class DeleteEntityTests : OsdrTest
    {
        readonly Guid _fileId;
        readonly int _fileVersion;
        readonly Guid _parentFolderId;
        readonly Guid _childFolderId1;
        readonly Guid _childFolderId2;

        public DeleteEntityTests(OsdrFixture fixture) : base(fixture)
        {
            _parentFolderId = Guid.NewGuid();
            _childFolderId1 = Guid.NewGuid();
            _childFolderId2 = Guid.NewGuid();

            Harness.CreateFolder(_parentFolderId, "parent folder 1", UserId, UserId).Wait();
            Harness.CreateFolder(_childFolderId1, "child folder 1", _parentFolderId, UserId).Wait();
            Harness.CreateFolder(_childFolderId2, "child folder 2", _parentFolderId, UserId).Wait();

            var blobId = LoadBlob(UserId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", _childFolderId1 } }).Result;

            var file = Files.Find(new BsonDocument("Blob._id", blobId)).Project("{}").FirstOrDefault();

            _fileId = file["_id"].AsGuid;
            _fileVersion = file["Version"].AsInt32;
        }

        //[Fact(DisplayName = "Delete entity. When delete folder it should be deleted")]
        public async Task When_delete_folder_it_should_be_deleted()
        {
            var res = await Harness.DeleteFolder(_parentFolderId, UserId, 1);
            res.Should().BeTrue();

            var folder = await Session.Get<Folder>(_parentFolderId);
            folder.IsDeleted.Should().BeTrue();
            folder.UpdatedBy.Should().Be(UserId);
            folder.UpdatedDateTime.Should().NotBe(folder.CreatedDateTime);

            var folderView = await Folders.Find(new BsonDocument("_id", _parentFolderId)).FirstOrDefaultAsync() as IDictionary<string, object>;
            folderView.Should().EntityShouldBeEquivalentTo(folder);

            var nodeView = await Nodes.Find(new BsonDocument("_id", folder.Id)).FirstOrDefaultAsync() as IDictionary<string, object>;
            nodeView.Where(kvp => kvp.Key != "IsDeleted").ToDictionary(k => k.Key, v => v.Value)
                .Should().NodeShouldBeEquivalentTo(folder);
            nodeView["IsDeleted"].As<bool>().Should().BeTrue();
        }

        //[Fact(DisplayName = "Delete entity. When delete file it should be deleted")]
        public async Task When_delete_file_it_should_be_deleted()
        {
            var res = await Harness.DeleteFile(_fileId, UserId, _fileVersion);
            res.Should().BeTrue();

            var file = await Session.Get<File>(_fileId);
            file.IsDeleted.Should().BeTrue();
            file.UpdatedBy.Should().Be(UserId);
            file.UpdatedDateTime.Should().NotBe(file.CreatedDateTime);

            var fileView = await Files.Find(new BsonDocument("_id", _fileId)).FirstOrDefaultAsync() as IDictionary<string, object>;
            fileView.Where(kvp => !new[] { "IsDeleted", "TotalRecords", "Fields" }.Contains(kvp.Key)).ToDictionary(k => k.Key, v => v.Value)
                .Should().EntityShouldBeEquivalentTo(file);

            var nodeView = await Nodes.Find(new BsonDocument("_id", _fileId)).FirstOrDefaultAsync() as IDictionary<string, object>;
            nodeView.Where(kvp => !new[] { "IsDeleted", "TotalRecords", "Fields" }.Contains(kvp.Key)).ToDictionary(k => k.Key, v => v.Value)
                .Should().NodeShouldBeEquivalentTo(file);
            nodeView["IsDeleted"].As<bool>().Should().BeTrue();
        }

        //[Fact(DisplayName = "Delete entity. When delete record it should be deleted")]
        public async Task When_delete_record_it_should_be_deleted()
        {
            var record = await Records.Find(new BsonDocument("FileId", _fileId))
                .Project<DeletedEntity>("{IsDeleted:1, Version:1}")
                .FirstOrDefaultAsync();

            var res = await Harness.DeleteRecord(record.Id, UserId, record.Version);
            res.Should().BeTrue();

            (await Records.Find(new BsonDocument("_id", record.Id))
                .Project<DeletedEntity>("{IsDeleted:1}")
                .FirstOrDefaultAsync())
                .IsDeleted.Should().BeTrue();

            (await GetNode(record.Id))
                .IsDeleted.Should().BeTrue();
        }

        //[Fact(DisplayName = "Delete entity. When delete folder: sub-folders should be deleted")]
        public async Task When_delete_folder_subfolder_should_be_deleted()
        {
            var res = await Harness.DeleteFolder(_parentFolderId, UserId, 1);
            res.Should().BeTrue();

            var childFolderViews = await Folders.Find(new BsonDocument("ParentId", _parentFolderId)).ToListAsync();
            childFolderViews.Should().HaveCount(2);

            foreach (IDictionary<string, object> childFolderView in childFolderViews)
            {
                var childFolder = await Session.Get<Folder>((Guid)childFolderView["_id"]);
                childFolder.IsDeleted.Should().BeTrue();
                childFolder.UpdatedBy.Should().Be(UserId);
                childFolder.UpdatedDateTime.Should().NotBe(childFolder.CreatedDateTime);

                childFolderView.Should().EntityShouldBeEquivalentTo(childFolder);

                var childNodeView = await Nodes.Find(new BsonDocument("_id", childFolder.Id)).FirstOrDefaultAsync() as IDictionary<string, object>;

                childNodeView.Where(kvp => kvp.Key != "IsDeleted").ToDictionary(k => k.Key, v => v.Value)
                    .Should().NodeShouldBeEquivalentTo(childFolder);

                childNodeView["IsDeleted"].As<bool>().Should().BeTrue();
            }
        }

        //[Fact(DisplayName = "Delete entity. When delete folder: files inside should be deleted")]
        public async Task When_delete_folder_files_inside_should_be_deleted()
        {
            var res = await Harness.DeleteFolder(_parentFolderId, UserId, 1);
            res.Should().BeTrue();

            (await Files.Find(new BsonDocument("_id", _fileId))
                .Project<DeletedEntity>("{IsDeleted:1}")
                .FirstOrDefaultAsync())
                .IsDeleted.Should().BeTrue();

            (await GetNode(_fileId))
                .IsDeleted.Should().BeTrue();
        }

        //[Fact(DisplayName = "Delete entity. When delete file: records inside should be deleted")]
        public async Task When_delete_file_records_inside_should_be_deleted()
        {
            var res = await Harness.DeleteFile(_fileId, UserId, _fileVersion);
            res.Should().BeTrue();

            (await Records.Find(new BsonDocument("FileId", _fileId))
                .Project<DeletedEntity>("{IsDeleted:1}")
                .ToListAsync())
                .Should().NotBeEmpty()
                     .And.HaveCount(1)
                     .And.OnlyContain(x => x.IsDeleted);

            (await GetNodes(_fileId))
                .Should().NotBeEmpty()
                     .And.HaveCount(1)
                     .And.OnlyContain(x => x.IsDeleted);
        }

        private async Task<DeletedEntity> GetNode(Guid id)
        {
            return await Nodes.Find(new BsonDocument("_id", id))
                .Project<DeletedEntity>("{IsDeleted:1}")
                .FirstOrDefaultAsync();
        }

        private async Task<IEnumerable<DeletedEntity>> GetNodes(Guid parentId)
        {
            return await Nodes.Find(new BsonDocument("ParentId", parentId))
                .Project<DeletedEntity>("{IsDeleted:1}")
                .ToListAsync();
        }

        public class DeletedEntity
        {
            public Guid Id { get; set; }
            public bool IsDeleted { get; set; }
            public int Version { get; set; }
        }
    }
}
