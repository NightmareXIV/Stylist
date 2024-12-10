using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist;
public class ForcedItemInfo
{
    public RowRef<Item> Item;
    public uint MaxLevel;
    public uint Power;

    public ForcedItemInfo(uint item, uint maxLevel, uint power)
    {
        Item = new RowRef<Item>(Svc.Data.Excel, item);
        MaxLevel = maxLevel;
        Power = power;
    }
}
