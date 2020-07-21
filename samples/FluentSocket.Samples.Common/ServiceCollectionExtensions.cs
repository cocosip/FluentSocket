using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Scheduling;
using FluentSocket.Samples.Common.Serializing;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSocket.Samples.Common
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSamples(this IServiceCollection services)
        {
            services
                .AddSingleton<IScheduleService, ScheduleService>()
                .AddSingleton<IPerformanceService, DefaultPerformanceService>()
                .AddTransient<IJsonSerializer, DefaultJsonSerializer>()
                .AddTransient<IBinarySerializer, DefaultBinarySerializer>();
            return services;
        }

    }
}
