using Microsoft.Extensions.DependencyInjection;
using System;

namespace FluentSocket
{
    public static class ServiceProviderExtensions
    {

        internal static T CreateInstance<T>(this IServiceProvider provider, params object[] args)
        {
            return (T)ActivatorUtilities.CreateInstance(provider, typeof(T), args);
        }

    }
}
