using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist.Gui;
public unsafe static class TabDebug
{
    public static void Draw()
    {
        ImGuiEx.TreeNodeCollapsingHeader("Gearset module", () => 
        {
            var r = RaptureGearsetModule.Instance();
            ImGuiEx.Text($"{r->EnabledGearsetIndex2EntryIndex.ToArray().Print()}");
            for(int i = 0; i < r->Entries.Length; i++)
            {
                if(!r->IsValidGearset(i)) continue;
                var entry = *r->GetGearset(i);
                if(ImGui.CollapsingHeader($"Entry#{i} | {entry.Id} | {entry.NameString}"))
                {
                    ImGuiEx.Text($"valid: {r->IsValidGearset(i)}");
                    for(int q = 0; q < entry.Items.Length && q < Utils.GearsetSlotMap.Length; q++)
                    {
                        var item = entry.Items[q];
                        ImGuiEx.Text($"{ExcelItemHelper.GetName(item.ItemId % 1000000, true)} - hq:{item.ItemId > 1000000}, candidate: {Utils.GetBestItemForJob((Job)entry.ClassJob, Utils.GearsetSlotMap[q])}");
                    }
                    if(ImGui.Button($"Update {i}"))
                    {
                        Utils.UpdateGearsetIfNeeded(i);
                    }
                    ImGui.SameLine();

                    if(ImGui.Button($"Update armory only {i}"))
                    {
                        Utils.UpdateGearsetIfNeeded(i, false);
                    }
                }
            }
        });
        ImGuiEx.TreeNodeCollapsingHeader("Jobs", () =>
        {
            foreach(var x in Enum.GetValues<Job>().Where(x => !x.IsUpgradeable() && x != Job.ADV))
            {
                if(ImGui.CollapsingHeader($"{x}"))
                {
                    ImGuiEx.Text($"Best items (restriction): \n{Utils.EquipSlots.Select(q => $"{q}: {Utils.GetBestItemForJob(x, [q])}").Print("\n")}");
                    ImGui.Separator();
                    ImGuiEx.Text($"Best items (no restriction): \n{Utils.EquipSlots.Select(q => $"{q}: {Utils.GetBestItemForJob(x, [q], true)}").Print("\n")}");
                    ImGui.Separator();
                    foreach(var s in Utils.CheckedBaseParams)
                    {
                        ImGuiEx.Text(x.GetBaseParamPrio(s) > 0 ? EColor.GreenBright : null, $"{s}: {x.GetBaseParamPrio(s)}");
                    }
                }
            }
        });
    }
}
