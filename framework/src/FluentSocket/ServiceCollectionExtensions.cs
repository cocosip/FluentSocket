using Microsoft.Extensions.DependencyInjection;

namespace FluentSocket
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFluentSocket(this IServiceCollection services)
        {
            services
                .AddSingleton<IFluentSocketFactory, FluentSocketFactory>()
                .AddScoped<ServerSetting>()
                .AddScoped<ClientSetting>()
                ;

            return services;
        }

    }
}
