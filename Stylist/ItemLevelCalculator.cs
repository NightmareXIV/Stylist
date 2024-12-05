using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist;
public unsafe static class ItemLevelCalculator
{
    private static readonly uint[] canHaveOffhand = [2, 6, 8, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32];
    private static readonly uint[] ignoreCategory = [105];

    internal static int? Calculate(RaptureGearsetModule.GearsetEntry entry)
    {
        var sum = 0u;
        var c = 12;
        for(var i = 0; i < 13; i++)
        {
            if(i == 5) continue;
            var slot = entry.Items[i];
            var id = slot.ItemId % 1000000;
            if(!Svc.Data.GetExcelSheet<Item>().TryGetRow(id, out var item)) continue;
            if(ignoreCategory.ContainsNullable(item.ItemUICategory.RowId))
            {
                if(i == 0) c -= 1;
                c -= 1;
                continue;
            }

            if(i == 0 && !canHaveOffhand.ContainsNullable(item.ItemUICategory.RowId))
            {
                sum += item.LevelItem.RowId;
                i++;
            }
            sum += item.LevelItem.RowId;
        }

        var avgItemLevel = sum / c;
        return (int)avgItemLevel;
    }

}
