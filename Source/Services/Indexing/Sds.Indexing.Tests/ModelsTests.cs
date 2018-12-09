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
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Events;
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
    public class ModelsTests : IClassFixture<IndexingFixture>, IDisposable
    {
        Guid _userId = Guid.NewGuid();
        IMongoDatabase _mongoDb;
        IBlobStorage _blobStorage = new InMemoryStorage();
        Mock<IElasticClient> _elasticClientMock;
        IList<IIndexRequest<object>> _fakeIndex;
        dynamic _model;
        Guid _modelId;
        string index;
        string type;
        IDictionary<string, object> doc;
        bool? docAsUpsert;
        BusTestHarness _harness;
        ConsumerTestHarness<ModelEventHandler> _consumer;

        public ModelsTests(IndexingFixture indexingFixture)
        {
            _modelId = Guid.NewGuid();
            index = string.Empty;
            type = string.Empty;
            doc = null;
            docAsUpsert = null;
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();
            _fakeIndex = new List<IIndexRequest<dynamic>>();
            _mongoDb = indexingFixture.MongoDb;
            var models = _mongoDb.GetCollection<BsonDocument>("Models");
            models.InsertOne(
                new BsonDocument("_id", _modelId)
                .Add("Name", "Processed.mol")
                .Add("Status", "Processed")
                .Add(_someField, _someValue));

            var list = new List<BsonDocument>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new BsonDocument("_id", Guid.NewGuid()).Add("Name", i.ToString()));
            }

            models.InsertMany(list);
            _model = _mongoDb.GetCollection<dynamic>("Models").Find(new BsonDocument("_id", _modelId)).First();
            ((IDictionary<string, object>)_model).Remove("_id");
            ((IDictionary<string, object>)_model).Add("id", _modelId);

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
            _consumer = _harness.Consumer(() => new ModelEventHandler(_elasticClientMock.Object, _mongoDb, _blobStorage));

            _harness.Start().Wait();

        }

        [Fact]
        public async Task When_model_persited_it_shoud_wirtten_to_elasticsearch()
        {
            await _harness.InputQueueSendEndpoint.Send<ModelPersisted>(new { Id = _model.id, UserId = _userId });

            await _consumer.Consumed.Any<ModelPersisted>();

            var request = new IndexRequest<object>(_model, "models", "model", _model.id);

            _fakeIndex.First().ShouldBeEquivalentTo(request);

            index.Should().Be("models");
            type.Should().Be("model");
        }

        [Fact]
        public async Task When_model_status_persited_model_shoud_be_written_to_elasticsearch()
        {
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            var nodes = _mongoDb.GetCollection<BsonDocument>("Nodes");
            await nodes.InsertOneAsync(
                new BsonDocument("_id", _model.id)
                .Add("Name", "TestName1")
                .Add("Status", "Processed")
                .Add(_someField, _someValue));

            ((IDictionary<string, object>)_model).Add("Node", new Dictionary<string, object>
            {
                { "id", _model.id },
                { "Name", "TestName1"},
                { "Status", "Processed" },
                { _someField, _someValue}
            });

            await _harness.InputQueueSendEndpoint.Send<ModelStatusPersisted>(new
            {
                Id = _model.id,
                UserId = _userId,
                TimeStamp = DateTimeOffset.UtcNow,
                Status = ModelStatus.Processed
            });

            _consumer.Consumed.Select<ModelStatusPersisted>().Any();
            
            _elasticClientMock.Verify(m => m.IndexAsync<object>(It.IsAny<IndexRequest<object>>(), null, default(CancellationToken)));

            var request = new IndexRequest<object>(_model, "models", "model", _model.id);
            _fakeIndex.First().ShouldBeEquivalentTo(request);

            index.Should().Be("models");
            type.Should().Be("model");
            
            //doc.ShouldBeEquivalentTo((IDictionary<string, object>)_file);
        }

        [Fact]
        public async Task When_new_model_name_persited_file_shoud_be_wirtten_to_elasticsearch()
        {
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            var nodes = _mongoDb.GetCollection<BsonDocument>("Nodes");
            await nodes.InsertOneAsync(
                new BsonDocument("_id", _model.id)
                .Add("Name", "TestName1")
                 .Add("Status", "Processed")
                .Add(_someField, _someValue));

            var name = "TestName1";

            ((IDictionary<string, object>)_model).Add("Node", new Dictionary<string, object>
            {
                { "id", _model.id },
                { "Name", "TestName1"},
                { "Status", "Processed" },
                { _someField, _someValue}
            });

            await _harness.InputQueueSendEndpoint.Send<ModelNamePersisted>(new
            {
                Id = _model.id,
                UserId =_userId,
                Name = name,
                TimeStamp = DateTimeOffset.UtcNow
            });

            _consumer.Consumed.Select<ModelNamePersisted>().Any();

            _elasticClientMock.Verify(m => m.IndexAsync<object>(It.IsAny<IndexRequest<object>>(), null, default(CancellationToken)));

            index.Should().Be("models");
            type.Should().Be("model");
            var request = new IndexRequest<object>(_model, "models", "model", _model.id);
            _fakeIndex.First().ShouldBeEquivalentTo(request);

            //doc.ShouldBeEquivalentTo((IDictionary<string, object>)_file);
        }

        [Fact]
        public async Task When_new_model_parent_persited_file_shoud_be_wirtten_to_elasticsearch()
        {
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            var nodes = _mongoDb.GetCollection<BsonDocument>("Nodes");
            await nodes.InsertOneAsync(
                new BsonDocument("_id", _model.id)
                .Add("Name", "TestName1")
                .Add("Status", "Processed")
                .Add(_someField, _someValue));

            ((IDictionary<string, object>)_model).Add("Node", new Dictionary<string, object>
            {
                { "id", _model.id },
                { "Name", "TestName1"},
                { "Status", "Processed" },
                { _someField, _someValue}
            });

            await _harness.InputQueueSendEndpoint.Send<ModelParentPersisted>(new
            {
                Id = _model.id,
                UserId =_userId,
                TimeStamp = DateTimeOffset.UtcNow
            });
            _consumer.Consumed.Select<ModelParentPersisted>().Any();

            _elasticClientMock.Verify(m => m.IndexAsync<object>(It.IsAny<IndexRequest<object>>(), null, default(CancellationToken)));

            var request = new IndexRequest<object>(_model, "models", "model", _model.id);
            _fakeIndex.First().ShouldBeEquivalentTo(request);

            index.Should().Be("models");
            type.Should().Be("model");

            //doc.ShouldBeEquivalentTo((IDictionary<string, object>)_file);
        }

        [Fact]
        public async Task When_model_deleted_it_shoud_deleted_from_elasticsearch()
        {
            _elasticClientMock.Setup(m => m.DeleteAsync(It.IsAny<IDeleteRequest>(), default(CancellationToken)))
                .Returns(Task.FromResult(new Mock<IDeleteResponse>().Object));

            await _harness.InputQueueSendEndpoint.Send(new ModelDeleted(_model.id, _userId));

            _consumer.Consumed.Select<ModelDeleted>().Any();

            _elasticClientMock.Verify(m => m.DeleteAsync(
                It.Is<IDeleteRequest>(r => r.Id == _modelId && r.Index.Name == "models" && r.Type.Name == "model"), default(CancellationToken)));
        }

        public void Dispose()
        {
            _harness.Dispose();
        }
    }
}
