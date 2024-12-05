using Dalamud.Plugin;
using ECommons;
using ECommons.Configuration;
using ECommons.SimpleGui;
using ECommons.Singletons;
using Stylist.Configuration;

namespace Stylist;

public class Stylist : IDalamudPlugin
{
    public static Stylist P;
    private Config Config;
    public static Config C => P.Config;
    public Stylist(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this);
        new TickScheduler(() =>
        {
            Config = EzConfig.Init<Config>();
            EzCmd.Add("/stylist", EzConfigGui.Open, "Open the plugin's UI");
            SingletonServiceManager.Initialize(typeof(S));
        });
    }

    public void Dispose()
    {
        ECommonsMain.Dispose();
    }
}
