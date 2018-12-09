using Microsoft.Extensions.DependencyInjection;
using Sds.Osdr.Domain.Modules;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sds.Osdr.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddOsdrModules(this IServiceCollection services, params Assembly[] assemblies)
        {
            var moduleTypes = assemblies
                .SelectMany(x => x.DefinedTypes)
                .Where(type => typeof(IModule).IsAssignableFrom(type.AsType()))
                .Select(t => t.AsType());

            foreach (var type in moduleTypes)
            {
                services.AddTransient(typeof(IModule), type);
            }
        }

        public static void UseInMemoryOsdrModules(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IServiceCollection), "UseInMemoryModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(services, new object[] { services });
            }
        }

        public static void UseBackEndOsdrModules(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IServiceCollection), "UseBackEndModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(services, new object[] { services });
            }
        }

        public static void UseFrontEndOsdrModules(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IServiceCollection), "UseFrontEndModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(services, new object[] { services });
            }
        }

        public static void UsePersistenceOsdrModules(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IServiceCollection), "UsePersistenceModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(services, new object[] { services });
            }
        }

        public static void UseSagaHostOsdrModules(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var addModuleMethods = assemblies.GetExtensionMethods(typeof(IServiceCollection), "UseSagaHostModule");

            foreach (var addModule in addModuleMethods)
            {
                addModule.Invoke(services, new object[] { services });
            }
        }

        //public static void AddBackEndConsumers(this IServiceCollection services, params Assembly[] assemblies)
        //{
        //    services.AddConsumers<IBackEndConsumer>(assemblies);
        //}

        //public static void AddFrontEndConsumers(this IServiceCollection services, params Assembly[] assemblies)
        //{
        //    services.AddConsumers<IFrontEndConsumer>(assemblies);
        //}

        //public static void AddPersistenceConsumers(this IServiceCollection services, params Assembly[] assemblies)
        //{
        //    services.AddConsumers<IPersistenceConsumer>(assemblies);
        //}

        private static void AddConsumers<T>(this IServiceCollection services, params Assembly[] assemblies)
        {
            var consumers = assemblies
                .SelectMany(x => x.DefinedTypes)
                .Where(type => typeof(T).IsAssignableFrom(type.AsType()))
                .Select(t => t.AsType());

            foreach (var consumer in consumers)
            {
                services.AddScoped(consumer);
            }
        }
    }
}
