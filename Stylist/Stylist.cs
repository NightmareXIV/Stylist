using Dalamud.Plugin;
using ECommons;

namespace Stylist;

public class Stylist : IDalamudPlugin
{
    public static Stylist P;
    public Stylist(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this);
    }

    public void Dispose()
    {
        ECommonsMain.Dispose();
    }
}
