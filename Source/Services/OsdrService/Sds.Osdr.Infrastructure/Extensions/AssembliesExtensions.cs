using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sds.Osdr.Infrastructure.Extensions
{
    public static class AssembliesExtensions
    {
        public static IEnumerable<MethodInfo> GetExtensionMethods(this IEnumerable<Assembly> assemblies, Type extendedType, string methodName)
        {
            return assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(type => type.IsSealed &&
                                    !type.IsGenericType &&
                                    !type.IsNested)
                    .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    .Where(method => method.IsDefined(typeof(ExtensionAttribute), false) &&
                                        method.GetParameters()[0].ParameterType == extendedType &&
                                        method.Name.Equals(methodName));
        }
    }
}
