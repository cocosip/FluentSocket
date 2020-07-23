namespace FluentSocket.DotNetty
{
    public static class SettingExtensions
    {
        public static ServerSetting AddDotNettySetting(this ServerSetting setting, DotNettyServerSetting extraSetting)
        {
            setting.ExtraSettings.Add(extraSetting);
            return setting;
        }

        public static ClientSetting AddDotNettySetting(this ClientSetting setting, DotNettyClientSetting extraSetting)
        {
            setting.ExtraSettings.Add(extraSetting);
            return setting;
        }
    }
}
