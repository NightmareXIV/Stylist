using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Stylist;
public unsafe struct InventoryDescriptor : IEquatable<InventoryDescriptor>
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

    public override bool Equals(object obj)
    {
        return obj is InventoryDescriptor descriptor && Equals(descriptor);
    }

    public bool Equals(InventoryDescriptor other)
    {
        return Type == other.Type &&
               Slot == other.Slot &&
               IsHQ == other.IsHQ;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Slot, IsHQ);
    }

    public InventoryItem GetSlot()
    {
        return *InventoryManager.Instance()->GetInventoryContainer(Type)->GetInventorySlot(Slot);
    }

    public override string ToString()
    {
        return $"[{Type}|{Slot}] {ExcelItemHelper.GetName(GetSlot().ItemId, true)} - hq: {IsHQ}";
    }

    public static bool operator ==(InventoryDescriptor left, InventoryDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(InventoryDescriptor left, InventoryDescriptor right)
    {
        return !(left == right);
    }
}
