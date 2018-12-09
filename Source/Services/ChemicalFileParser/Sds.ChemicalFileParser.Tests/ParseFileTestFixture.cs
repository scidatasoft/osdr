using MassTransit;
using MassTransit.Testing;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalFileParser.Processing.CommandHandlers;
using Sds.MassTransit.Extensions;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace Sds.ChemicalFileParser.Tests
{
    public class ParseFileTestFixture : IDisposable
    {
        private BusTestHarness _harness;

        public Guid UserId { get; private set; }
        public BusTestHarness Harness { get { return _harness; } }
        public IBlobStorage BlobStorage { get; } = new InMemoryStorage();

        public ParseFileTestFixture()
        {
            UserId = Guid.NewGuid();

            var settings = ConfigurationManager.AppSettings.TestHarnessSettings();

            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(settings.Timeout);

            _harness.Handler<RecordParsed>(context => { return Task.CompletedTask; });
            _harness.Handler<RecordParseFailed>(context => { return Task.CompletedTask; });
            _harness.Handler<FileParsed>(context => { return Task.CompletedTask; });
            _harness.Handler<FileParseFailed>(context => { return Task.CompletedTask; });

            _harness.OnConfigureBus += cfg =>
            {
                cfg.ReceiveEndpoint("test", e =>
                {
                    e.Consumer(() => new ParseFileCommandHandler(BlobStorage));
                });
            };

            _harness.Start().Wait();
        }

        public void Dispose()
        {
            _harness.Stop().Wait();
        }
    }
}
