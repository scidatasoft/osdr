using MassTransit.RabbitMqTransport;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sds.Osdr.Infrastructure.Extensions
{
    public static class RabbitMqBusFactoryConfiguratorExtensions
    {
        public static void RegisterBackEndOsdrModules(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider container, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IRabbitMqBusFactoryConfigurator), "RegisterBackEndModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(configurator, new object[] { configurator, host, container, endpointConfigurator });
            }
        }

        public static void RegisterFrontEndOsdrModules(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider container, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IRabbitMqBusFactoryConfigurator), "RegisterFrontEndModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(configurator, new object[] { configurator, host, container, endpointConfigurator });
            }
        }

        public static void RegisterPersistenceOsdrModules(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider container, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IRabbitMqBusFactoryConfigurator), "RegisterPersistenceModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(configurator, new object[] { configurator, host, container, endpointConfigurator });
            }
        }

        public static void RegisterSagaHostOsdrModules(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider container, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IRabbitMqBusFactoryConfigurator), "RegisterSagaHostModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(configurator, new object[] { configurator, host, container, endpointConfigurator });
            }
        }
    }
}
