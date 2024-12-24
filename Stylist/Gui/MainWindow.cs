using ECommons.ImGuiMethods.TerritorySelection;
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
            ("Settings", DrawSettings, null, true),
            InternalLog.ImGuiTab(),
            ("Debug", TabDebug.Draw, ImGuiColors.DalamudGrey3, true)
            );
    }

    void DrawSettings()
    {
        ImGui.Checkbox($"Consider gear from inventory", ref C.UseInventory);
        ImGui.Checkbox($"Re-equip current gearset if it was updated", ref C.Reequip);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Search, "Check For Suggestions"))
        {
            P.CheckForSuggestions();
        }
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Bell, "Configure Notification Zones"))
        {
            new TerritorySelector(C.NotifyTerr, (sel, x) =>
            {
                C.NotifyTerr = x;
            });
        }
    }
}
