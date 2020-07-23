using Microsoft.Extensions.DependencyInjection;

namespace FluentSocket.SuperSocket
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFluentSocketSuperSocket(this IServiceCollection services)
        {
            //services
            //    .AddScoped<ISocketServer, DotNettySocketServer>()
            //    .AddScoped<ISocketClient, DotNettySocketClient>()
            //    ;
            return services;
        }
    }
}
