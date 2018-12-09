using FluentAssertions;
using MassTransit.Testing;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Nest;
using Newtonsoft.Json.Linq;
using Sds.Indexing.EventHandlers;
using Sds.MassTransit.Extensions;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sds.Indexing.Tests
{
    public class RecordTests:IClassFixture<IndexingFixture>, IDisposable
    {
        IndexingFixture _fixture;
        dynamic _record;
        Guid _recordId;
        BusTestHarness _harness;
        ConsumerTestHarness<RecordEventHandler> _consumer;
        IMongoCollection<BsonDocument> nodes => _fixture.MongoDb.GetCollection<BsonDocument>("Nodes");

        public RecordTests(IndexingFixture fixture)
        {
            _fixture = fixture;
            _recordId = Guid.NewGuid();
            _fixture.FakeIndex.Clear();

            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            var records = _fixture.MongoDb.GetCollection<BsonDocument>("Records");
           

            records.InsertOne(
                new BsonDocument("_id", _recordId)
                .Add("Name", "TestName1")
                .Add("Status", "Processed")
                .Add(_someField, _someValue)
                );

           

            var list = new List<BsonDocument>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new BsonDocument("_id", Guid.NewGuid()).Add("name", i.ToString()));
            }

            records.InsertMany(list);
            _record = _fixture.MongoDb.GetCollection<dynamic>("Records").Find(new BsonDocument("_id", _recordId)).First();
            ((IDictionary<string, object>)_record).Remove("_id");
            ((IDictionary<string, object>)_record).Add("id", _recordId);

           

            _harness = new InMemoryTestHarness();
            _consumer = _harness.Consumer(() => new RecordEventHandler(fixture.ElasticClientMock.Object, fixture.MongoDb));
            _harness.Start().Wait();
        }

        [Fact]
        public async Task When_record_persited_it_shoud_wirtten_to_elasticsearch()
        {
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            await _harness.InputQueueSendEndpoint.Send<RecordPersisted>(new
            {
                _record.id,
                _fixture.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });

            _consumer.Consumed.Select<RecordPersisted>().Any().Should().BeTrue();

            _fixture.FakeIndex.Should().HaveCount(1);

            var request = new IndexRequest<object>(_record, "records", "record", _record.id);
            _fixture.FakeIndex.First().ShouldBeEquivalentTo(request);
        }
        
        [Fact]
        public async Task When_record_status_persited_record_shoud_be_wirtten_to_elasticsearch()
        {
            string _someField = Guid.NewGuid().ToString();
            string _someValue = Guid.NewGuid().ToString();

            nodes.InsertOne(
              new BsonDocument("_id", _recordId)
              .Add("Name", "TestName1")
              .Add("Status", "Processed")
              .Add(_someField, _someValue)
              );
            var _node = _fixture.MongoDb.GetCollection<dynamic>("Nodes").Find(new BsonDocument("_id", _recordId)).First();
            ((IDictionary<string, object>)_node).Remove("_id");
            ((IDictionary<string, object>)_node).Add("id", _recordId);

            ((IDictionary<string, object>)_record).Add("Node", _node);
            await _harness.InputQueueSendEndpoint.Send<StatusPersisted>(new
            {
                id = _record.id,
                Status = RecordStatus.Processed,
                Userid = _fixture.UserId,
                TimeStamp = DateTimeOffset.UtcNow
            });

            _consumer.Consumed.Select<StatusPersisted>().Any().Should().BeTrue();

            var request = new IndexRequest<object>(_record, "records", "record", _record.id);
            _fixture.FakeIndex.Should().HaveCount(1);
            _fixture.FakeIndex.First().ShouldBeEquivalentTo(request);
        }

        [Fact]
        public async Task When_record_deleted_it_shoud_deleted_from_elasticsearch()
        {
            _fixture.ElasticClientMock.Setup(m => m.DeleteAsync(It.IsAny<IDeleteRequest>(), default(CancellationToken)))
                .Returns(Task.FromResult(new Mock<IDeleteResponse>().Object));

            await _harness.InputQueueSendEndpoint.Send(new RecordDeleted(_record.id, RecordType.Structure, _fixture.UserId));

            _consumer.Consumed.Select<RecordDeleted>().Any().Should().BeTrue();

            _fixture.ElasticClientMock.Verify(m => m.DeleteAsync(
                It.Is<IDeleteRequest>(r => r.Id == _recordId && r.Index.Name == "records" && r.Type.Name == "record"), default(CancellationToken)), Times.Once);
        }

        public void Dispose()
        {
            _harness.Dispose();
        }
    }
}
