using FluentAssertions;
using MassTransit.Testing;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.MassTransit.Extensions;
using Sds.MetadataStorage.Domain.Commands;
using Sds.MetadataStorage.Domain.Events;
using Sds.MetadataStorage.Processing.EventHandlers;
using Sds.Osdr.RecordsFile.Sagas.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.MetadataStorage.Tests
{
    public class FileEventHandlersTests : IClassFixture<TestFixture>
    {
        readonly TestFixture _fixture;
        readonly ConsumerTestHarness<FileEventHandlers> _fileEventsConsumer;
        readonly IMongoCollection<BsonDocument> _records;
        readonly IMongoCollection<BsonDocument> _files;
        readonly IMongoCollection<BsonDocument> _metadatas;
        readonly Guid _testFileId;

        public FileEventHandlersTests(TestFixture fixture)
        {
            _fixture = fixture;

            _records = _fixture.Database.GetCollection<BsonDocument>("Records");
            _files = _fixture.Database.GetCollection<BsonDocument>("Files");
            _metadatas = _fixture.Database.GetCollection<BsonDocument>("Metadata");

            _testFileId = Guid.NewGuid();
            _files.InsertOne(new
            {
                _id = _testFileId,
                Properties = new { Fields = new[] { "F1" } }
            }.ToBsonDocument());

            _fileEventsConsumer = _fixture.Harness.Consumer(() => new FileEventHandlers(_fixture.Database));

            _fixture.Harness.Start().Wait();
        }

        [Fact]
        public async Task BooleanTests()
        {
            string[] booleans = new[] { "true", "false", "1", "0", "y", "n", "yes", "no", "YES", "True", "N" };
           
            var rnd = new Random();
            var records = new List<BsonDocument>();
            for (int i = 0; i < 11_000; i++)
            {
                records.Add(new
                {
                    FileId = _testFileId,
                    Properties = new
                    {
                        Fields = new[]
                        {
                            new
                            {
                                Name="F1",
                                Value = booleans[rnd.Next(booleans.Length-1)].ToString()
                            }
                        }
                    }
                }.ToBsonDocument());
            }
            await _records.InsertManyAsync(records);

            await _fixture.Harness.InputQueueSendEndpoint.Send<GenerateMetadata>(new { FileId = _testFileId });

            var publishedEvent = _fixture.Harness.Published.Select<MetadataGenerated>().FirstOrDefault(m => m.Context.Message.Id == _testFileId);
            publishedEvent.Should().NotBeNull();

            var generatedDoc = await _metadatas.Find(new BsonDocument("_id", _testFileId)).FirstOrDefaultAsync();
            var expectedDoc = new
            {
                _id = _testFileId,
                Fields = new[] { new { Name = "F1", DataType = "boolean" }.ToBsonDocument() }
            }.ToBsonDocument();

            (generatedDoc == expectedDoc).Should().BeTrue();
        }

        [Fact]
        public async Task IntegerTests()
        {
            var rnd = new Random();
            var records = new List<BsonDocument>();
            var ints = new List<int>();
            for (int i = 0; i < 11_000; i++)
            {
                int value = rnd.Next(7000);
                ints.Add(value);
                records.Add(new
                {
                    FileId = _testFileId,
                    Properties = new
                    {
                        Fields = new[] { new { Name = "F1", Value = value.ToString() } }
                    }
                }.ToBsonDocument());
            }
            await _records.InsertManyAsync(records);

            await _fixture.Harness.InputQueueSendEndpoint.Send<GenerateMetadata>(new { FileId = _testFileId });

            var message = _fixture.Harness.Published.Select<MetadataGenerated>().FirstOrDefault(m => m.Context.Message.Id == _testFileId);
            message.Should().NotBeNull();

            var generatedDoc = await _metadatas.Find(new BsonDocument("_id", _testFileId)).FirstOrDefaultAsync();
            var expectedDoc = new
            {
                _id = _testFileId,
                Fields = new[]
                {
                    new
                    {
                        Name = "F1",
                        DataType = "integer",
                        MinValue = ints.Min(),
                        MaxValue = ints.Max()
                    }.ToBsonDocument()
                }
            }.ToBsonDocument();
            (generatedDoc == expectedDoc).Should().BeTrue();
        }

        [Fact(Skip = "unstable")]
        public async Task DecimalTests()
        {
            var rnd = new Random();
            var records = new List<BsonDocument>();
            var decimals = new List<decimal>();
            for (int i = 0; i < 11_000; i++)
            {
                decimal value = ((decimal)rnd.Next(30_000)) / 3;
                decimals.Add(value);
                records.Add(new
                {
                    FileId = _testFileId,
                    Properties = new
                    {
                        Fields = new[] { new { Name = "F1", Value = value.ToString() } }
                    }
                }.ToBsonDocument());
            }
            await _records.InsertManyAsync(records);

            await _fixture.Harness.InputQueueSendEndpoint.Send<GenerateMetadata>(new { FileId = _testFileId });
            
            var message = _fixture.Harness.Published.Select<MetadataGenerated>().FirstOrDefault(m => m.Context.Message.Id == _testFileId);
            message.Should().NotBeNull();

            var generatedDoc = await _metadatas.Find(new BsonDocument("_id", _testFileId)).FirstOrDefaultAsync();
            var expectedDoc = new
            {
                _id = _testFileId,
                Fields = new[]
                {
                    new
                    {
                        Name = "F1",
                        DataType = "decimal",
                        MinValue = (object)decimals.Min(),
                        MaxValue = (object)decimals.Max()
                    }.ToBsonDocument()
                }
            }.ToBsonDocument();

           (generatedDoc == expectedDoc).Should().BeTrue();
        }

        [Fact]
        public async Task StringsTests()
        {
            var rnd = new Random();
            var records = new List<BsonDocument>();

            for (int i = 0; i < 1_000; i++)
                records.Add(new BsonDocument("FileId", _testFileId).Add("Properties", new BsonDocument("Fields", new BsonArray(new[] { new { Name = "F1", Value = i.ToString() }.ToBsonDocument() }))));

            for (int i = 0; i < 1_000; i++)
            {
                decimal value = ((decimal)rnd.Next(777)) / 3;
                records.Add(new BsonDocument("FileId", _testFileId).Add("Properties", new BsonDocument("Fields", new BsonArray(new[] { new { Name = "F1", Value = value.ToString(NumberFormatInfo.InvariantInfo) }.ToBsonDocument() }))));
            }

            records.Add(new BsonDocument("FileId", _testFileId).Add("Properties", new BsonDocument("Fields", new BsonArray(new[] { new { Name = "F1", Value = "string value" }.ToBsonDocument() }))));

            await _records.InsertManyAsync(records);

            await _fixture.Harness.InputQueueSendEndpoint.Send<GenerateMetadata>(new { FileId = _testFileId });
            
            var message = _fixture.Harness.Published.Select<MetadataGenerated>().FirstOrDefault(m => m.Context.Message.Id == _testFileId);

            message.Should().NotBeNull();
            var doc = await _metadatas.Find(new BsonDocument("_id", _testFileId)).FirstOrDefaultAsync();

            var doc1 = new BsonDocument("_id", _testFileId).Add("Fields", new BsonArray(new[] { new { Name = "F1", DataType = "string" }.ToBsonDocument() }));

            (doc == doc1).Should().BeTrue();
        }

        [Fact]
        public async Task EmptyFieldsTest()
        {
            await _fixture.Harness.InputQueueSendEndpoint.Send<GenerateMetadata>(new { FileId = _testFileId });

            var message = _fixture.Harness.Published.Select<MetadataGenerated>().FirstOrDefault(m => m.Context.Message.Id == _testFileId);
            message.Should().NotBeNull();

            var generatedDoc = await _metadatas.Find(new BsonDocument("_id", _testFileId)).FirstOrDefaultAsync();
            var expectedDoc = new
            {
                _id = _testFileId,
                Fields = new[] { new { Name = "F1", DataType = "string" }.ToBsonDocument() }
            }.ToBsonDocument();

            (generatedDoc == expectedDoc).Should().BeTrue();
        }
    }
}