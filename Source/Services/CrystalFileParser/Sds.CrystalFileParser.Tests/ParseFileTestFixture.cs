using Autofac;
using MassTransit;
using MassTransit.Testing;
using Sds.CrystalFileParser.Domain.Events;
using Sds.MassTransit.AutofacIntegration;
using Sds.MassTransit.Extensions;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;
using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;

namespace Sds.ChemicalFileParser.Tests
{
    public class ParseFileTestFixture : IDisposable
    {
        private BusTestHarness _harness;
        public BusTestHarness Harness { get { return _harness; } }
        public Guid UserId { get; private set; }
        public IBlobStorage BlobStorage { get; private set; }

        public ParseFileTestFixture()
        {
            UserId = Guid.NewGuid();

            var builder = new ContainerBuilder();

            builder.RegisterConsumers(Assembly.Load("Sds.CrystalFileParser.Processing"));

            builder.RegisterType<InMemoryStorage>()
                .As<IBlobStorage>()
                .SingleInstance();

            var settings = ConfigurationManager.AppSettings.TestHarnessSettings();

            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(settings.Timeout);

            _harness.Handler<RecordParsed>(context => { return Task.CompletedTask; });
            _harness.Handler<FileParsed>(context => { return Task.CompletedTask; });
            _harness.Handler<FileParseFailed>(context => { return Task.CompletedTask; });

            var container = builder.Build();

            _harness.OnConfigureBus += cfg =>
            {
                cfg.ReceiveEndpoint("test", e =>
                {
                    e.LoadFrom(container);
                });
            };

            BlobStorage = container.Resolve<IBlobStorage>();
        }

        public void Dispose()
        {
        }
    }
}
