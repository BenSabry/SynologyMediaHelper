using Microsoft.Extensions.Configuration;


public static class SettingsHelper
{
    internal static readonly string appsettingsPath = Path.Combine(CommonHelper.GetBaseDirectory(), "AppSettings.json");
    public static Settings GetSettings()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(appsettingsPath)
            .Build();

        var temp = new Settings(config);
        config.Bind(temp);

        return temp;
    }
}
