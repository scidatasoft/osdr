using FluentAssertions;
using MassTransit.Testing;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Nest;
using Sds.Indexing.EventHandlers;
using Sds.MassTransit.Extensions;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Events.Files;
using Sds.Osdr.RecordsFile.Domain.Events.Files;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sds.Indexing.Tests
{
    public class FileTests : IClassFixture<IndexingFixture>, IDisposable
    {
        Guid _userId = Guid.NewGuid();
        IMongoDatabase _mongoDb;
        IBlobStorage _blobStorage = new InMemoryStorage();
        Mock<IElasticClient> _elasticClientMock;
        IList<IIndexRequest<object>> _fakeIndex;
        dynamic _file;
        Guid _fileId;
        string index;
        string type;
        IDictionary<string, object> doc;
        bool? docAsUpsert;
        BusTestHarness _harness;
        ConsumerTestHarness<FileEventHandler> _consumer;

        public FileTests(IndexingFixture indexingFixture)
        {
            _fileId = Guid.NewGuid();
            index = string.Empty;
            type = string.Empty;
            doc = null;
            docAsUpsert = null;
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();
            _fakeIndex = new List<IIndexRequest<dynamic>>();
            _mongoDb = indexingFixture.MongoDb;
            var files = _mongoDb.GetCollection<BsonDocument>("Files");
            files.InsertOne(
                new BsonDocument("_id", _fileId)
                .Add("Name", "Processed.mol")
                .Add("Status", "Processed")
                .Add(_someField, _someValue));

            var list = new List<BsonDocument>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new BsonDocument("_id", Guid.NewGuid()).Add("Name", i.ToString()));
            }

            files.InsertMany(list);
            _file = _mongoDb.GetCollection<dynamic>("Files").Find(new BsonDocument("_id", _fileId)).First();
            ((IDictionary<string, object>)_file).Remove("_id");
            ((IDictionary<string, object>)_file).Add("id", _fileId);

            _elasticClientMock = new Mock<IElasticClient>();
            _elasticClientMock
                .Setup(m => m.IndexAsync<object>(It.IsAny<IIndexRequest>(), null, default(CancellationToken)))
                .Returns(Task.FromResult(new Mock<IIndexResponse>().Object))
                .Callback<IIndexRequest<object>, Func<IndexDescriptor<object>, IIndexRequest>, CancellationToken>((a, s, c) => {
                    _fakeIndex.Add(a);
                    index = a.Index.Name;
                    type = a.Type.Name;
                    }).Verifiable();

            _elasticClientMock.Setup(m => m.UpdateAsync<object, object>(It.IsAny<IUpdateRequest<object, object>>(), default(CancellationToken)))
                .Returns(Task.FromResult(new Mock<IUpdateResponse<object>>().Object))
            .Callback<IUpdateRequest<object, object>, CancellationToken>((a, c) =>
            {
                index = a.Index.Name;
                type = a.Type.Name;
                docAsUpsert = a.DocAsUpsert;
                doc = a.Doc as IDictionary<string, object>;
            }).Verifiable();

            _harness = new InMemoryTestHarness();
            _consumer = _harness.Consumer(() => new FileEventHandler(_elasticClientMock.Object, _mongoDb, _blobStorage));

            _harness.Start().Wait();

        }

        [Fact(Skip = "need change")]
        public async Task When_file_persited_it_shoud_wirtten_to_elasticsearch()
        {
            await _harness.InputQueueSendEndpoint.Send(new FilePersisted(_file.id, _userId, null, null));

            await _consumer.Consumed.Any<FilePersisted>();

            var request = new IndexRequest<object>(_file, "files", "file", _file.id);
           
            //_fixture.FakeIndex.Should().HaveCount(1);
            //_fixture.FakeIndex.First().ShouldBeEquivalentTo(request);
        }

        [Fact]
        public async Task When_FileStatus_persited_file_shoud_be_wirtten_to_elasticsearch()
        {
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            var nodes = _mongoDb.GetCollection<BsonDocument>("Nodes");
            await nodes.InsertOneAsync(
                new BsonDocument("_id", _file.id)
                .Add("Name", "TestName1")
                .Add("Status", "Processed")
                .Add(_someField, _someValue));

            ((IDictionary<string, object>)_file).Add("Node", new Dictionary<string, object>
            {
                { "id", _file.id },
                { "Name", "TestName1"},
                { "Status", "Processed" },
                { _someField, _someValue}
            });

            await _harness.InputQueueSendEndpoint.Send<NodeStatusPersisted>(new
            {
                _file.id,
                _userId,
                TimeStamp = DateTimeOffset.UtcNow,
                Status = FileStatus.Processed
            });

            _consumer.Consumed.Select<StatusPersisted>().Any();
            
            _elasticClientMock.Verify(m => m.IndexAsync<object>(It.IsAny<IndexRequest<object>>(), null, default(CancellationToken)));

            var request = new IndexRequest<object>(_file, "files", "file", _file.id);
            _fakeIndex.First().ShouldBeEquivalentTo(request);

            index.Should().Be("files");
            type.Should().Be("file");
            
            //doc.ShouldBeEquivalentTo((IDictionary<string, object>)_file);
        }

        [Fact]
        public async Task When_new_file_name_persited_file_shoud_be_wirtten_to_elasticsearch()
        {
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            var nodes = _mongoDb.GetCollection<BsonDocument>("Nodes");
            await nodes.InsertOneAsync(
                new BsonDocument("_id", _file.id)
                .Add("Name", "TestName1")
                 .Add("Status", "Processed")
                .Add(_someField, _someValue));

            ((IDictionary<string, object>)_file).Add("Node", new Dictionary<string, object>
            {
                { "id", _file.id },
                { "Name", "TestName1"},
                { "Status", "Processed" },
                { _someField, _someValue}
            });

            await _harness.InputQueueSendEndpoint.Send<FileNamePersisted>(new
            {
                _file.id,
                _userId,
                TimeStamp = DateTimeOffset.UtcNow
            });
            _consumer.Consumed.Select<FileNamePersisted>().Any();

            _elasticClientMock.Verify(m => m.IndexAsync<object>(It.IsAny<IndexRequest<object>>(), null, default(CancellationToken)));

            index.Should().Be("files");
            type.Should().Be("file");
            var request = new IndexRequest<object>(_file, "files", "file", _file.id);
            _fakeIndex.First().ShouldBeEquivalentTo(request);

            //doc.ShouldBeEquivalentTo((IDictionary<string, object>)_file);
        }

        [Fact]
        public async Task When_new_file_parent_persited_file_shoud_be_wirtten_to_elasticsearch()
        {
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            var nodes = _mongoDb.GetCollection<BsonDocument>("Nodes");
            await nodes.InsertOneAsync(
                new BsonDocument("_id", _file.id)
                .Add("Name", "TestName1")
                .Add("Status", "Processed")
                .Add(_someField, _someValue));

            ((IDictionary<string, object>)_file).Add("Node", new Dictionary<string, object>
            {
                { "id", _file.id },
                { "Name", "TestName1"},
                { "Status", "Processed" },
                { _someField, _someValue}
            });

            await _harness.InputQueueSendEndpoint.Send<FileParentPersisted>(new
            {
                _file.id,
                _userId,
                TimeStamp = DateTimeOffset.UtcNow
            });
            _consumer.Consumed.Select<FileParentPersisted>().Any();

            _elasticClientMock.Verify(m => m.IndexAsync<object>(It.IsAny<IndexRequest<object>>(), null, default(CancellationToken)));

            var request = new IndexRequest<object>(_file, "files", "file", _file.id);
            _fakeIndex.First().ShouldBeEquivalentTo(request);

            index.Should().Be("files");
            type.Should().Be("file");

            //doc.ShouldBeEquivalentTo((IDictionary<string, object>)_file);
        }

        [Fact]
        public async Task When_file_deleted_it_shoud_deleted_from_elasticsearch()
        {
            _elasticClientMock.Setup(m => m.DeleteAsync(It.IsAny<IDeleteRequest>(), default(CancellationToken)))
                .Returns(Task.FromResult(new Mock<IDeleteResponse>().Object));

            await _harness.InputQueueSendEndpoint.Send(new FileDeleted(_file.id, FileType.Generic, _userId));

            _consumer.Consumed.Select<FileDeleted>().Any();

            _elasticClientMock.Verify(m => m.DeleteAsync(
                It.Is<IDeleteRequest>(r => r.Id == _fileId && r.Index.Name == "files" && r.Type.Name == "file"), default(CancellationToken)));
        }

        public void Dispose()
        {
            _harness.Dispose();
        }
    }
}
