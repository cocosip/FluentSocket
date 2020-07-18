namespace FluentSocket.DotNetty
{
    public class DotNettyClientSetting : DotNettySetting
    {
        /// <summary>Group
        /// </summary>
        public int GroupEventLoopCount { get; set; } = 1;

        /// <summary>Keep alive
        /// </summary>
        public bool SoKeepalive { get; set; } = false;
    }
}
