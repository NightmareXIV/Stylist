using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist.Gui;
public unsafe class MainWindow : ConfigWindow
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
            P.CheckForSuggestions(true);
        }
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Bell, "Configure Notification Zones"))
        {
            new TerritorySelector(C.NotifyTerr, (sel, x) =>
            {
                C.NotifyTerr = x;
            });
        }
        if(ImGui.CollapsingHeader("Blacklisted Gearsets"))
        {
            if(Player.CID != 0)
            {
                if(!C.BlacklistedGearsets.TryGetValue(Player.CID, out var list))
                {
                    C.BlacklistedGearsets[Player.CID] = [];
                    list = C.BlacklistedGearsets[Player.CID];
                }

                var rgs = RaptureGearsetModule.Instance();
                for(int i = 0; i < rgs->Entries.Length; i++)
                {
                    var entry = rgs->Entries[i];
                    if(rgs->IsValidGearset(i))
                    {
                        ImGuiEx.CollectionCheckbox($"{entry.NameString}##{i}", i, list);
                    }
                }
            }
        }
    }
}
