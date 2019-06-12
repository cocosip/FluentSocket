using Microsoft.Extensions.DependencyInjection;
using System;

namespace FluentSocket
{
    public static class ServiceProviderExtensions
    {
        public static object CreateInstance(this IServiceProvider provider, Type type, params object[] args)
        {
            return ActivatorUtilities.CreateInstance(provider, type, args);
        }

        public static T CreateInstance<T>(this IServiceProvider provider, params object[] args)
        {
            return (T)ActivatorUtilities.CreateInstance(provider, typeof(T), args);
        }

    }
}
