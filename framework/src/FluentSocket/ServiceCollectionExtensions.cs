using FluentSocket.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSocket
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFluentSocket(this IServiceCollection services)
        {
            services.AddTransient<IChannelManager, ChannelManager>();
            services.AddSingleton<IScheduleService, ScheduleService>();
            return services;
        }
    }
}
