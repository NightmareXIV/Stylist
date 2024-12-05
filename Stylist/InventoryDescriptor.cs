using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist;
public unsafe struct InventoryDescriptor
{
    public RowRef<Item> Data;
    public InventoryType Type;
    public int Slot;
    public bool IsHQ;

    public InventoryDescriptor(InventoryType type, int slot) : this()
    {
        Type = type;
        Slot = slot;
        var item = GetSlot();
        Data = new RowRef<Item>(Svc.Data.Excel, item.ItemId % 1000000);
        IsHQ = item.ItemId > 1000000;
    }

    public InventoryItem GetSlot()
    {
        return *InventoryManager.Instance()->GetInventoryContainer(Type)->GetInventorySlot(Slot);
    }

    public override string ToString()
    {
        return $"[{Type}|{Slot}] {ExcelItemHelper.GetName(GetSlot().ItemId)}";
    }
}
