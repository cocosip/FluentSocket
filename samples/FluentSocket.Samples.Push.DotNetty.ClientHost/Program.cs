using FluentSocket.DotNetty;
using FluentSocket.Samples.Common;
using FluentSocket.Samples.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Push.DotNetty.ClientHost
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            services
                .AddLogging(l =>
                {
                    l.SetMinimumLevel(LogLevel.Debug);
                    l.AddConsole();
                })
                .AddSamples()
                .AddFluentSocket()
                .AddFluentSocketDotNetty()
                .Configure<PushHandleOption>(c =>
                {
                    c.Setting = new ClientSetting()
                    {
                        EnableHeartbeat = true,
                        EnableReConnect = true,
                        ReConnectMaxCount = 1000,
                        ReConnectDelaySeconds = 1,
                        ServerEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22000),
                        PushReqCapacity = 20000,
                        HandlePushReqThread = 1
                    };
                });

            var provider = services.BuildServiceProvider();
            var handleService = provider.GetRequiredService<PushHandleService>();
            Task.Run(async () =>
            {
                await handleService.Start();
            });

            Console.ReadLine();
        }


    }
}
