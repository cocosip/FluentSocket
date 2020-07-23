using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Scheduling;
using FluentSocket.Samples.Common.Serializing;
using FluentSocket.Samples.Common.Services;
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
                .AddTransient<IBinarySerializer, DefaultBinarySerializer>()
                .AddSingleton<MessageSendService>()
                .AddSingleton<MessageHandleService>()
                .AddSingleton<PushSendService>()
                .AddSingleton<PushHandleService>()
                .Configure<MessageSendOption>(c => { })
                .Configure<MessageHandleOption>(c => { })
                .Configure<PushSendOption>(c => { })
                .Configure<PushHandleOption>(c => { })
                ;
            return services;
        }

    }
}
