using FluentSocket.TestCommon.Performance;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSocket.TestCommon
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>添加性能计数
        /// </summary>
        public static IServiceCollection AddPerformance(this IServiceCollection services)
        {
            return services.AddSingleton<IPerformanceService, DefaultPerformanceService>();
        }
    }
}
