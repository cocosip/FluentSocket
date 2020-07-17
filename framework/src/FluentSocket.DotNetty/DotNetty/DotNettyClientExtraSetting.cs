namespace FluentSocket.DotNetty
{
    public class DotNettyClientExtraSetting : DotNettyExtraSetting, IExtraSetting
    {
        /// <summary>Group
        /// </summary>
        public int GroupEventLoopCount { get; set; } = 1;
    }
}
