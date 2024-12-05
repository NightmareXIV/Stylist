using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist.Services;
public unsafe class ItemMover : IDisposable
{
    public List<InventoryDescriptor> ItemsToMove = [];
    public int Attempts = 0;
    private ItemMover()
    {
        Svc.Framework.Update += Framework_Update;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= Framework_Update;
    }

    private void Framework_Update(Dalamud.Plugin.Services.IFramework framework)
    {
        if(ItemsToMove.Count > 0)
        {
            if(!Player.Available)
            {
                ItemsToMove.Clear();
                return;
            }
            if(!IsScreenReady()) return;
            var next = ItemsToMove[0];
            var item = InventoryManager.Instance()->GetInventoryContainer(next.Type)->GetInventorySlot(next.Slot);
            if(item->GetItemId() == 0)
            {
                ItemsToMove.RemoveAt(0);
                Attempts = 0;
                return;
            }
            if(Attempts>10)
            {
                PluginLog.Warning($"Too many move attempts, skipping move of {next.Type}/{next.Slot}");
                ItemsToMove.RemoveAt(0);
                Attempts = 0;
                return;
            }
            if(item->GetItemId() != next.Data.RowId + (next.IsHQ ? 1000000 : 0))
            {
                PluginLog.Warning($"Requested item mismatch, skipping {next.Type}/{next.Slot}");
                ItemsToMove.RemoveAt(0);
                Attempts = 0;
                return;
            }
            var targetInventory = (EquipSlotCategoryEnum)next.Data.Value.EquipSlotCategory.RowId switch
            {
                EquipSlotCategoryEnum.WeaponTwoHand => InventoryType.ArmoryMainHand,
                EquipSlotCategoryEnum.WeaponMainHand => InventoryType.ArmoryMainHand,
                EquipSlotCategoryEnum.OffHand => InventoryType.ArmoryOffHand,
                EquipSlotCategoryEnum.Head => InventoryType.ArmoryHead,
                EquipSlotCategoryEnum.Body => InventoryType.ArmoryBody,
                EquipSlotCategoryEnum.Gloves => InventoryType.ArmoryHands,
                EquipSlotCategoryEnum.Legs => InventoryType.ArmoryLegs,
                EquipSlotCategoryEnum.Feet => InventoryType.ArmoryFeets,
                EquipSlotCategoryEnum.Ears => InventoryType.ArmoryEar,
                EquipSlotCategoryEnum.Neck => InventoryType.ArmoryNeck,
                EquipSlotCategoryEnum.Wrists => InventoryType.ArmoryWrist,
                EquipSlotCategoryEnum.Ring => InventoryType.ArmoryRings,
                _ => default,
            };
            if(targetInventory == default)
            {
                PluginLog.Warning($"Can't find suitable inventory, skipping {next.Type}/{next.Slot}");
                ItemsToMove.RemoveAt(0);
                Attempts = 0;
                return;
            }
            if(targetInventory == next.Type)
            {
                PluginLog.Warning($"Can't move to the same inventory, skipping {next.Type}/{next.Slot}");
                ItemsToMove.RemoveAt(0);
                Attempts = 0;
                return;
            }
            var targetSlot = -1;
            var cont = InventoryManager.Instance()->GetInventoryContainer(targetInventory);
            for(int i = 0; i < cont->GetSize(); i++)
            {
                if(cont->GetInventorySlot(i)->ItemId == 0)
                {
                    targetSlot = i; 
                    break;
                }
            }

            if(targetSlot == -1)
            {
                PluginLog.Warning($"Can't find free slot in {targetInventory}, skipping {next.Type}/{next.Slot}");
                ItemsToMove.RemoveAt(0);
                Attempts = 0;
                return;
            }
            if(EzThrottler.Throttle("MoveItem"))
            {
                PluginLog.Information($"Move item from {next.Type}/{next.Slot} to {targetInventory}/{targetSlot}");
                InventoryManager.Instance()->MoveItemSlot(next.Type, (ushort)next.Slot, targetInventory, (ushort)targetSlot, 1);
                Attempts++;
            }
        }
    }
}
