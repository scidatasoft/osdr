using MassTransit.Testing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.MetadataStorage.Tests
{
    public class TestFixture : IDisposable
    {
        public BusTestHarness Harness { get; private set; } = new InMemoryTestHarness();
        public IMongoDatabase Database { get; private set; } = new MongoClient(Environment.ExpandEnvironmentVariables("%OSDR_MONGO_DB%")).GetDatabase($"metadata_test_{Guid.NewGuid()}");

        public TestFixture()
        {
            var decimalSerializer = new DecimalSerializer(BsonType.Decimal128, new RepresentationConverter(allowOverflow: false, allowTruncation: false));
            BsonSerializer.RegisterSerializer(decimalSerializer);
        }

        void IDisposable.Dispose()
        {
            Database.Client.DropDatabase(Database.DatabaseNamespace.DatabaseName);
            Harness.Dispose();
        }
    }
}
