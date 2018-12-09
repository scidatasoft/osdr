using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using MassTransit;
using MassTransit.Testing;
using Sds.Cvsp;
using Sds.Cvsp.Compounds;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;
using System.Reflection;

namespace Sds.ChemicalStandardizationValidation.Tests
{
    public class StandardizationValidationTestFixture : IDisposable
    {
        private BusTestHarness _harness;
        public BusTestHarness Harness { get { return _harness; } }
        public Guid UserId { get; private set; }
        public IBlobStorage BlobStorage { get; private set; }

        public StandardizationValidationTestFixture()
        {
            UserId = Guid.NewGuid();

            var builder = new ContainerBuilder();

            builder.RegisterConsumers(Assembly.Load("Sds.ChemicalStandardizationValidation.Processing"));

            builder.RegisterType<InMemoryStorage>()
                .As<IBlobStorage>()
                .SingleInstance();

            builder.RegisterModule<Sds.Cvsp.Compounds.Autofac.ValidationStandardizationModule>();
            builder.Register(c => new XmlIssuesConfig(CompoundResources.LogCodes)).As<IIssuesConfig>();

            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(30);

            builder.RegisterModule<Cvsp.Compounds.Autofac.PropertiesCalculationModule>();

            var container = builder.Build();

            ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocator(container));

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
