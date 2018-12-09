using MassTransit;
using MassTransit.Testing;
using Sds.ChemicalExport.Domain;
using Sds.ChemicalExport.FileRecordsProvider;
using Sds.ChemicalExport.Processing.CommandHandlers;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;

namespace Sds.ChemicalExport.Tests
{
    public class ExportTestFixture : IDisposable
    {
        private BusTestHarness _harness;

        public Guid UserId { get; private set; }
        public BusTestHarness Harness { get { return _harness; } }
        public IBlobStorage BlobStorage { get; } = new InMemoryStorage();
        public IRecordsProvider provider { get; }

        public ExportTestFixture()
        {
            UserId = Guid.NewGuid();

            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(5);

            provider = new BlobStorageRecordsProvider(BlobStorage);

            _harness.OnConfigureBus += cfg =>
            {
                cfg.ReceiveEndpoint("test", e =>
                {
                    e.Consumer(() => new ExportFileCommandHandler(BlobStorage, provider));
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
