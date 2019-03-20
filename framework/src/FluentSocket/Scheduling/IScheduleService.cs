using System;

namespace FluentSocket
{
    public interface IScheduleService
    {
        void StartTask(string name, Action action, int dueTime, int period);
        void StopTask(string name);
    }
}
