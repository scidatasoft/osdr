using MassTransit;
using MassTransit.Testing;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using Sds.WebImporter.PdfProcessing.CommandHandlers;
using System;

namespace Sds.WebImporter.PdfTests
{
    public class ConverterFixture : IDisposable
    {
        private BusTestHarness _harness;

        public Guid UserId { get; private set; }
        public BusTestHarness Harness { get { return _harness; } }
        public IBlobStorage BlobStorage { get; } = new InMemoryStorage();

        public ConverterFixture()
        {
            UserId = Guid.NewGuid();

            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(5);

            _harness.OnConfigureBus += cfg =>
            {
                cfg.ReceiveEndpoint("test", e =>
                {
                    e.Consumer(() => new GeneratePdfFromHtmlCommandHandler(BlobStorage));
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
