using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Serializing;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSocket.Samples.Common
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>添加性能计数
        /// </summary>
        public static IServiceCollection AddPerformance(this IServiceCollection services)
        {
            return services.AddSingleton<IPerformanceService, DefaultPerformanceService>();
        }

        public static IServiceCollection AddSerialize(this IServiceCollection services)
        {
            services
               .AddTransient<IJsonSerializer, DefaultJsonSerializer>()
               .AddTransient<IBinarySerializer, DefaultBinarySerializer>();
            return services;
        }

    }
}
