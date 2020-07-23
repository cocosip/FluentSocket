using Microsoft.Extensions.DependencyInjection;
using System;

namespace FluentSocket
{
    public static class ServiceProviderExtensions
    {
        public static T CreateInstance<T>(this IServiceProvider serviceProvider, params object[] args)
        {
            return (T)ActivatorUtilities.CreateInstance(serviceProvider, typeof(T), args);
        }
    }
}
