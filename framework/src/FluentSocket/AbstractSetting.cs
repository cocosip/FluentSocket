using System.Collections.Generic;

namespace FluentSocket
{
    public abstract class AbstractSetting
    {
        public List<IExtraSetting> ExtraSettings { get; set; }

        /// <summary>Round robin request expired interval (ms)
        /// </summary>
        public int ScanTimeoutRequestInterval { get; set; } = 1000;

        /// <summary>Wait the handler execute time (ms)
        /// </summary>
        public int WaitHandlerExecuteMilliSeconds { get; set; } = 3000;

        /// <summary>Send message flowControl threshold
        /// </summary>
        public int SendMessageFlowControlThreshold { get; set; } = 500;

        /// <summary>Quiet after connection channel close (ms)
        /// </summary>
        public int QuietPeriodMilliSeconds { get; set; } = 100;

        /// <summary>Timeout when close the channel (s)
        /// </summary>
        public int CloseTimeoutSeconds { get; set; } = 1;


        public AbstractSetting()
        {
            ExtraSettings = new List<IExtraSetting>();
        }
    }
}
