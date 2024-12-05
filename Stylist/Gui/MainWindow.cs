using ECommons.SimpleGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist.Gui;
public class MainWindow : ConfigWindow
{
    private MainWindow()
    {
        EzConfigGui.Init(this);
    }

    public override void Draw()
    {
        ImGuiEx.EzTabBar("Default",
            ("Settings", () => { }, null, true),
            InternalLog.ImGuiTab(),
            ("Debug", TabDebug.Draw, ImGuiColors.DalamudGrey3, true)
            );
    }
}
