using FluentSocket.DotNetty;
using FluentSocket.Samples.Common;
using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Message.DotNetty.ServerHost
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
                .Configure<MessageHandleOption>(c =>
                {
                    c.Setting = new ServerSetting()
                    {
                        ListeningEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21000),
                        HandleMessageReqThread = 1,
                        MessageReqCapacity = 50000,
                    };
                });

            var provider = services.BuildServiceProvider();
            var handleService = provider.GetRequiredService<MessageHandleService>();
            Task.Run(async () =>
            {
                await handleService.Start();
            });

            Console.ReadLine();
        }



    }
}
