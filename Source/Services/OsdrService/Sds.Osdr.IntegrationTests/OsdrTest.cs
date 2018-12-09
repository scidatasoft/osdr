using CQRSlite.Domain;
using CQRSlite.Events;
using MassTransit;
using MongoDB.Driver;
using Sds.Storage.Blob.Core;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    [CollectionDefinition("OSDR Test Harness")]
    public class OsdrTestCollection : ICollectionFixture<OsdrTestHarness>
    {
    }

    public abstract class OsdrTest
    {
        public OsdrTestHarness Harness { get; }

        protected Guid JohnId => Harness.JohnId;
        protected Guid JaneId => Harness.JaneId;
        protected IBus Bus => Harness.BusControl;
        protected ISession Session => Harness.Session;
        protected IBlobStorage BlobStorage => Harness.BlobStorage;
        protected IEventStore CqrsEventStore => Harness.CqrsEventStore;
        protected EventStore.IEventStore EventStore => Harness.EventStore;
        
        protected IMongoCollection<dynamic> Nodes => Harness.MongoDb.GetCollection<dynamic>("Nodes");
        protected IMongoCollection<dynamic> Files => Harness.MongoDb.GetCollection<dynamic>("Files");
        protected IMongoCollection<dynamic> Folders => Harness.MongoDb.GetCollection<dynamic>("Folders");
        protected IMongoCollection<dynamic> Users => Harness.MongoDb.GetCollection<dynamic>("Users");
        protected IMongoCollection<dynamic> Models => Harness.MongoDb.GetCollection<dynamic>("Models");
        protected IMongoCollection<dynamic> Records => Harness.MongoDb.GetCollection<dynamic>("Records");

        public OsdrTest(OsdrTestHarness fixture, ITestOutputHelper output = null)
        {
            Harness = fixture;

            if (output != null)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo
                    .TestOutput(output, LogEventLevel.Verbose)
                    .CreateLogger()
                    .ForContext<OsdrTest>();
            }
        }

        protected Guid GetBlobId(Guid fileId)
        {
            var fileCreated = Harness.Received.Select<Generic.Domain.Events.Files.FileCreated>(m => m.Context.Message.Id == fileId).First();
            return fileCreated.Context.Message.BlobId;
        }
    }
}