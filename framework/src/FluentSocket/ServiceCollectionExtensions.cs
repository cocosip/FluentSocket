using FluentSocket.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSocket
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFluentSocket(this IServiceCollection services)
        {
            services
                .AddSingleton<IFluentSocketFactory, DefaultFluentSocketFactory>()
                .AddSingleton<ISocketSessionBuilder, DefaultSocketSessionBuilder>()
                .AddTransient<ISocketSessionFactory, DefaultSocketSessionFactory>()
                .AddScoped<ServerSetting>()
                .AddScoped<ClientSetting>()
                ;

            return services;
        }

    }
}
