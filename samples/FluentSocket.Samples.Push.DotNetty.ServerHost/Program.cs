using FluentSocket.DotNetty;
using FluentSocket.Samples.Common;
using FluentSocket.Samples.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Push.DotNetty.ServerHost
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
                .Configure<PushSendOption>(c =>
                {
                    c.Setting = new ServerSetting()
                    {
                        ListeningEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22000),
                        HandleMessageReqThread = 1,
                        MessageReqCapacity = 50000,
                    };
                });

            var provider = services.BuildServiceProvider();
            var pushService = provider.GetRequiredService<PushSendService>();
            Task.Run(async () =>
            {
                await pushService.Start();
            });

            Console.ReadLine();
        }
      
    }
}
