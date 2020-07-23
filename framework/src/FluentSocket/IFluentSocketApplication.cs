using System;

namespace FluentSocket
{
    public interface IFluentSocketApplication
    {
        IServiceProvider ServiceProvider { get; }
    }
}
