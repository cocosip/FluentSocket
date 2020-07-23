using System.Collections.Generic;

namespace FluentSocket
{
    public abstract class Setting : ISetting
    {
        public List<ISetting> ExtraSettings { get; set; }

        /// <summary>Round robin request expired interval (ms)
        /// </summary>
        public int ScanTimeoutRequestInterval { get; set; } = 1000;

        /// <summary>Send message flowControl threshold
        /// </summary>
        public int SendMessageFlowControlThreshold { get; set; } = 500;

        /// <summary>Quiet after connection channel close (ms)
        /// </summary>
        public int QuietPeriodMilliSeconds { get; set; } = 100;

        /// <summary>Timeout when close the channel (s)
        /// </summary>
        public int CloseTimeoutSeconds { get; set; } = 1;


        public Setting()
        {
            ExtraSettings = new List<ISetting>();
        }
    }
}
