using MassTransit;
using MassTransit.Testing;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using Sds.WebImporter.ChemicalProcessing.CommandHandlers;
using System;

namespace Sds.WebImporter.Tests
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

            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(5);

            _harness.OnConfigureBus += cfg =>
            {
                cfg.ReceiveEndpoint("test", e =>
                {
                    e.Consumer(() => new ParseFileCommandHandler(BlobStorage));
                    //e.Consumer(() => new GeneratePdfFromHtmlCommandHandler(BlobStorage));
                    e.Consumer(() => new ProcessWebPageCommandHandler(BlobStorage));
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
