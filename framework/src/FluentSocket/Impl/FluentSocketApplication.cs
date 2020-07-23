using System;

namespace FluentSocket
{
    public class FluentSocketApplication : IFluentSocketApplication
    {
        public IServiceProvider ServiceProvider { get; }

        public FluentSocketApplication(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
    }
}
