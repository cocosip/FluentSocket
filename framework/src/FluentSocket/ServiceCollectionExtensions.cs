using FluentSocket.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSocket
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFluentSocket(this IServiceCollection services)
        {
            services
                .AddTransient<IChannelManager, ChannelManager>()
                .AddSingleton<IScheduleService, ScheduleService>()
                .AddSingleton<IFluentSocketFactory, FluentSocketFactory>();
            return services;
        }
    }
}
