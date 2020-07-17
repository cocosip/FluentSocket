namespace FluentSocket.DotNetty
{
    public static class SettingExtensions
    {
        public static ServerSetting AddDotNettySetting(this ServerSetting setting, DotNettyServerExtraSetting extraSetting)
        {
            setting.ExtraSettings.Add(extraSetting);
            return setting;
        }

        public static ClientSetting AddDotNettySetting(this ClientSetting setting, DotNettyClientExtraSetting extraSetting)
        {
            setting.ExtraSettings.Add(extraSetting);
            return setting;
        }
    }
}
