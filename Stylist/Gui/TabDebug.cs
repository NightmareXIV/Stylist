using ECommons.ExcelServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist.Gui;
public static class TabDebug
{
    public static void Draw()
    {
        foreach(var x in Enum.GetValues<Job>().Where(x => !x.IsUpgradeable() && x != Job.ADV))
        {
            if(ImGui.CollapsingHeader($"{x}"))
            {
                ImGuiEx.Text($"Best items (restriction): \n{Utils.EquipSlots.Select(q => $"{q}: {Utils.GetBestItemForJob(x, q)}").Print("\n")}");
                ImGui.Separator();
                ImGuiEx.Text($"Best items (no restriction): \n{Utils.EquipSlots.Select(q => $"{q}: {Utils.GetBestItemForJob(x, q, true)}").Print("\n")}");
                ImGui.Separator();
                foreach(var s in Utils.CheckedBaseParams)
                {
                    ImGuiEx.Text(x.GetBaseParamPrio(s)>0?EColor.GreenBright:null,$"{s}: {x.GetBaseParamPrio(s)}");
                }
            }
        }
    }
}
