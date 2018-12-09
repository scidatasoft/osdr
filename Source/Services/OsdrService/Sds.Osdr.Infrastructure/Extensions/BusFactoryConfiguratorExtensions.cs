using Automatonymous;
using MassTransit;
using MassTransit.Saga;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sds.Osdr.Infrastructure.Extensions
{
    public static class FactoryConfiguratorExtensions
    {
        public static void RegisterInMemoryOsdrModules(this IBusFactoryConfigurator configurator, IServiceProvider provider, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IBusFactoryConfigurator), "RegisterInMemoryModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(configurator, new object[] { configurator, provider });
            }
        }

        public static void RegisterStateMachine<TStateMachine, TState>(this IBusFactoryConfigurator configurator, IServiceProvider provider, Action<IReceiveEndpointConfigurator> endpointConfigurator = null)
            where TStateMachine : MassTransitStateMachine<TState>
            where TState : class, SagaStateMachineInstance
        {
            configurator.ReceiveEndpoint(typeof(TStateMachine).FullName, e =>
            {
                e.StateMachineSaga(provider.GetRequiredService<TStateMachine>(), provider.GetRequiredService<ISagaRepository<TState>>());

                endpointConfigurator?.Invoke(e);
            });
        }
    }
}
