using MassTransit;
using MassTransit.Testing;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Processing.CommandHandlers;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;
using System.Collections.Generic;

namespace Sds.Imaging.XUnitTests
{
    public class ImagingTestFixture : IDisposable
    {
        private BusTestHarness _harness;
        public BusTestHarness Harness { get { return _harness; } }
        public IBlobStorage BlobStorage { get; } = new InMemoryStorage();
        public string[] ImageFormatsForMolFile { get; } = { "bmp", "gif", "jpeg", "jpg", "png", "tiff", "svg" };

        public ImagingTestFixture()
        {
            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(30);

            _harness.OnConfigureBus += cfg =>
            {
                cfg.ReceiveEndpoint("test", e =>
                {
                    //e.Consumer(() => new ImagingCommandHandler(BlobStorage));
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
