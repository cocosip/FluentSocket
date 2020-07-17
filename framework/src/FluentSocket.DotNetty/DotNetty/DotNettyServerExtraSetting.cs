namespace FluentSocket.DotNetty
{
    public class DotNettyServerExtraSetting : DotNettyExtraSetting, IExtraSetting
    {
        /// <summary>BossGroup
        /// </summary>
        public int BossGroupEventLoopCount { get; set; } = 1;

        /// <summary>WorkGroup
        /// </summary>
        public int WorkGroupEventLoopCount { get; set; } = 1;

        /// <summary>SoBacklog
        /// </summary>
        public int SoBacklog { get; set; } = 128;
    }
}
