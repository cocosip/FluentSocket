using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSocket.DotNetty
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFluentSocketDotNetty(this IServiceCollection services)
        {
            services
                .AddScoped<ISocketServer, DotNettySocketServer>()
                .AddScoped<ISocketClient, DotNettySocketClient>()
                ;
            return services;
        }
    }
}
