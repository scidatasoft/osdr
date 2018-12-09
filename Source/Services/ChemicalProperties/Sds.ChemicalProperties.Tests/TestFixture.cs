using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using MassTransit;
using MassTransit.Testing;
//using Microsoft.Practices.ServiceLocation;
using Sds.ChemicalProperties.Processing.CommandHandlers;
using Sds.Cvsp.Compounds;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;

namespace Sds.ChemicalProperties.Tests
{
    public class TestFixture : IDisposable
    {
        private BusTestHarness _harness;
        public BusTestHarness Harness { get { return _harness; } }
        public IBlobStorage BlobStorage { get; } = new InMemoryStorage();
        public Guid UserId { get; } = NewId.NextGuid();

        public TestFixture()
        {
            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(30);

            var builder = new ContainerBuilder();

            builder.RegisterModule<Cvsp.Compounds.Autofac.PropertiesCalculationModule>();

            var container = builder.Build();

            ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(container));

            _harness.OnConfigureBus += cfg =>
            {
                cfg.ReceiveEndpoint("test", e =>
                {
                    e.Consumer(() => new ChemicalPropertiesCommandHandler(container.Resolve<PropertiesCalculation>(), BlobStorage));
                });
            };
        }

        public void Dispose()
        {
        }
    }
}
