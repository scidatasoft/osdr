using Autofac;
using MassTransit;
using MassTransit.Testing;
using Sds.OfficeProcessor.Domain.Commands;
using Sds.OfficeProcessor.Processing.CommandHandlers;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;
using System.Collections.Generic;
using System.Reflection;

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
            UserId = NewId.NextGuid();

            var builder = new ContainerBuilder();

            builder.RegisterConsumers(Assembly.Load("Sds.OfficeProcessor.Processing"));

            builder.RegisterType<InMemoryStorage>()
                .As<IBlobStorage>()
                .SingleInstance();

            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(30);

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
