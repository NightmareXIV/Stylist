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
        foreach(var x in Enum.GetValues<Job>().Where(x => !x.IsUpgradeable()))
        {
            if(ImGui.CollapsingHeader($"{x}"))
            {
                foreach(var s in Utils.CheckedBaseParams)
                {
                    ImGuiEx.Text(x.GetBaseParamPrio(s)>0?EColor.GreenBright:null,$"{s}: {x.GetBaseParamPrio(s)}");
                }
            }
        }
    }
}
