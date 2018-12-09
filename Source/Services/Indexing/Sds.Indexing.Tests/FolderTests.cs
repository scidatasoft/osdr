using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Nest;
using Sds.MassTransit.Extensions;
using Sds.Osdr.Generic.Domain.Events.Folders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sds.Indexing.Tests
{
    public class FolderTests:IClassFixture<IndexingFixture>
    {
        IndexingFixture _fixture;
        dynamic _folder;
        Guid _folderId;

        public FolderTests(IndexingFixture fixture)
        {
            _fixture = fixture;
            _folderId = Guid.NewGuid();

            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            var folders = _fixture.MongoDb.GetCollection<BsonDocument>("Folders");
            folders.InsertOneAsync(
                new BsonDocument("_id", _folderId)
                .Add("Name", "TestName1")
                .Add(_someField, _someValue))
                .Wait();

            var list = new List<BsonDocument>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new BsonDocument("_id", Guid.NewGuid()).Add("Name", i.ToString()));
            }

            folders.InsertMany(list);
            _folder = _fixture.MongoDb.GetCollection<dynamic>("Folders").Find(new BsonDocument("_id", _folderId)).First();
            ((IDictionary<string, object>)_folder).Remove("_id");
            ((IDictionary<string, object>)_folder).Add("id", _folderId);
        }

        [Fact]
        public async Task When_persited_folder_it_shoud_wirtten_to_elasticsearch()
        {
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();
           
            var nodes = _fixture.MongoDb.GetCollection<BsonDocument>("Nodes");
            nodes.InsertOneAsync(
                new BsonDocument("_id", _folder.id)
                .Add("Name", "TestName1")
                 .Add("Status", "Processed")
                .Add(_someField, _someValue))
                .Wait();

            await _fixture.Harness.Bus.Publish<FolderPersisted>(new { Id = _folder.id });

            await _fixture.Harness.Consumed.Any<FolderPersisted>();

            ((IDictionary<string, object>)_folder).Add("Node", new Dictionary<string, object>
            {
                { "id", _folder.id },
                { "Name", "TestName1"},
                { "Status", "Processed" },
                { _someField, _someValue}
            });

            var request = new IndexRequest<object>(_folder, "folders", "folder", _folder.id);
            _fixture.FakeIndex.Should().HaveCount(1);
            _fixture.FakeIndex.First().ShouldBeEquivalentTo(request);
        }

        [Fact]
        public async Task When_deleted_folder_it_shoud_deleted_from_elasticsearch()
        {
            _fixture.ElasticClientMock.Setup(m => m.DeleteAsync(It.IsAny<IDeleteRequest>(), default(CancellationToken)))
                .Returns(Task.FromResult(new Mock<IDeleteResponse>().Object));

            await _fixture.Harness.Bus.Publish(new FolderDeleted(_folder.id, Guid.Empty, _fixture.UserId));

            await _fixture.Harness.Consumed.Any<FolderDeleted>();

            _fixture.ElasticClientMock.Verify(m => m.DeleteAsync(
                It.Is<IDeleteRequest>(r => r.Id == _folderId && r.Index.Name == "folders" && r.Type.Name == "folder"), default(CancellationToken)), Times.Once);
        }
    }
}
