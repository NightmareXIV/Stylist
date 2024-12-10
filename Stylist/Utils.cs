using Dalamud.Memory;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using Stylist.Configuration;
using System.Diagnostics;

namespace Stylist;
public static unsafe class Utils
{
    public static readonly List<ForcedItemInfo> ForcedItems = [
        new(2632, 10, 20),
        new(2633,10,20),
        new(2634,10,20),
        new(8567,25, 20),
        new(14043, 30,30),
        new(16039,50,30),
        new(24589,70,30),
        new(31393,80,10),
        new(33648,80,30),
        new(41081,90,30),
        new(44410,60,30),
        ];

    public static readonly EquipSlotCategoryEnum[] EquipSlots = Enum.GetValues<EquipSlotCategoryEnum>().Where(x => (int)x <= 13).ToArray();

    /*
     * MainHand,
        OffHand,
        Head,
        Body,
        Hands,
        Belt,
        Legs,
        Feet,
        Ears,
        Neck,
        Wrists,
        RingRight,
        RingLeft,
        SoulStone
    */
    public static readonly EquipSlotCategoryEnum[][] GearsetSlotMap = [
        [EquipSlotCategoryEnum.WeaponMainHand, EquipSlotCategoryEnum.WeaponTwoHand],
        [EquipSlotCategoryEnum.OffHand],
        [EquipSlotCategoryEnum.Head],
        [EquipSlotCategoryEnum.Body],
        [EquipSlotCategoryEnum.Gloves],
        [EquipSlotCategoryEnum.Waist],
        [EquipSlotCategoryEnum.Legs],
        [EquipSlotCategoryEnum.Feet],
        [EquipSlotCategoryEnum.Ears],
        [EquipSlotCategoryEnum.Neck],
        [EquipSlotCategoryEnum.Wrists],
        [EquipSlotCategoryEnum.Ring],
        [EquipSlotCategoryEnum.Ring],
        ];

    public static void UpdateGearsetIfNeeded(int index, bool includeInventory = true)
    {
        var r = RaptureGearsetModule.Instance();
        var isCurrent = r->CurrentGearsetIndex == index;
        if(index < r->NumGearsets && r->IsValidGearset(index))
        {
            var entry = r->Entries.GetPointer(index);
            InventoryDescriptor? ring = null;
            for(int q = 0; q < entry->Items.Length && q < Utils.GearsetSlotMap.Length; q++)
            {
                var gsItem = entry->GetItem((RaptureGearsetModule.GearsetItemIndex)q);
                var candidate = Utils.GetBestItemForJob((Job)entry->ClassJob, Utils.GearsetSlotMap[q], true, q == 12?[ring]:null, includeInventory);
                if(q == 11) ring = candidate;
                if(candidate != null)
                {
                    if(candidate.Value.IsHQ == gsItem.ItemId > 1000000 && candidate.Value.GetSlot().GetItemId() == gsItem.ItemId % 1000000)
                    {
                        PluginLog.Debug($"Skipping existing item for slot {q}");
                    }
                    else
                    {
                        var t = entry->Items.GetPointer(q);
                        t->ItemId = candidate.Value.GetSlot().GetItemId();
                        t->GlamourId = candidate.Value.GetSlot().GetGlamourId();
                        t->Flags = 0;
                        t->Stain0Id = candidate.Value.GetSlot().GetStain(0);
                        t->Stain1Id = candidate.Value.GetSlot().GetStain(1);
                        MemoryHelper.WriteRaw((nint)t->Materia.GetPointer(0), MemoryHelper.ReadRaw((nint)candidate.Value.GetSlot().Materia.GetPointer(0), sizeof(ushort) * 5));
                        MemoryHelper.WriteRaw((nint)t->MateriaGrades.GetPointer(0), MemoryHelper.ReadRaw((nint)candidate.Value.GetSlot().MateriaGrades.GetPointer(0), sizeof(byte) * 5));
                        if(candidate.Value.Type.EqualsAny(InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4))
                        {
                            S.ItemMover.ItemsToMove.Add(candidate.Value);
                        }    
                        PluginLog.Debug($"Setting item for slot {q}");
                        if(isCurrent && C.Reequip)
                        {
                            PluginLog.Information($"Re-equipping gearset {index} once all items moved to armory chest");
                            P.TaskManager.Enqueue(() =>
                            {
                                if(S.ItemMover.ItemsToMove.Count > 0) return false;
                                if(!Player.Interactable) return false;
                                if(!Svc.ClientState.IsLoggedIn) return null;
                                RaptureGearsetModule.Instance()->EquipGearset(index);
                                return true;
                            }, new(showDebug:true));
                        }
                    }
                }
            }
            var mainHand = ExcelItemHelper.Get(entry->GetItem(RaptureGearsetModule.GearsetItemIndex.MainHand).ItemId % 1000000);
            if(mainHand != null && mainHand.Value.EquipSlotCategory.RowId == (uint)EquipSlotCategoryEnum.WeaponTwoHand)
            {
                var items = entry->Items;
                items.GetPointer((int)RaptureGearsetModule.GearsetItemIndex.OffHand)->ItemId = 0;
            }
            var ilvl = ItemLevelCalculator.Calculate(*entry);
            if(ilvl != null)
            {
                entry->ItemLevel = (short)ilvl.Value;
            }
        }
    }

    public static InventoryDescriptor? GetBestItemForJob(this Job job, EquipSlotCategoryEnum[] slot, bool restrictLevel = true, InventoryDescriptor?[] ignore = null, bool includeInventory = true)
    {
        InventoryDescriptor? ret = null;
        InventoryDescriptor? forcedRet = null;
        var maxLvl = PlayerState.Instance()->ClassJobLevels[Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)job).ExpArrayIndex];
        foreach(var type in includeInventory?ValidInventories:ValidInventoriesArmory)
        {
            var inv = InventoryManager.Instance()->GetInventoryContainer(type);
            for(int i = 0; i < inv->GetSize(); i++)
            {
                var item = inv->GetInventorySlot(i);
                if(item->GetItemId() != 0)
                {
                    var descriptor = new InventoryDescriptor(type, i);
                    if(ignore != null && ignore.Contains(descriptor)) continue;
                    if(descriptor.Data.ValueNullable != null && slot.Contains((EquipSlotCategoryEnum)descriptor.Data.Value.EquipSlotCategory.RowId) && descriptor.Data.Value.ClassJobCategory.Value.IsJobInCategory(job))
                    {
                        if(restrictLevel && descriptor.Data.Value.LevelEquip > maxLvl) continue;
                        if(ForcedItems.TryGetFirst(x => descriptor.Data.RowId == x.Item.RowId && x.MaxLevel <= maxLvl, out var newForcedItem))
                        {
                            var prevForcedItem = ForcedItems.FirstOrDefault(s => s.Item.RowId == ret?.Data.RowId);
                            if(prevForcedItem == null)
                            {
                                forcedRet = descriptor;
                            }
                            else
                            {
                                if(prevForcedItem.MaxLevel < newForcedItem.MaxLevel)
                                {
                                    forcedRet = descriptor;
                                }
                                else if(prevForcedItem.Power < newForcedItem.Power)
                                {
                                    forcedRet = descriptor;
                                }
                            }
                        }
                        else
                        {
                            if(ret == null || ret.Value.Data.Value.LevelItem.RowId < descriptor.Data.Value.LevelItem.RowId)
                            {
                                ret = descriptor;
                            }
                            else
                            {
                                if(GetBaseParamPrioForJob(job).Sum(x => (float)item->GetStat(x.Key) * x.Value) > GetBaseParamPrioForJob(job).Sum(x => (float)ret.Value.GetSlot().GetStat(x.Key) * x.Value))
                                {
                                    ret = descriptor;
                                }
                            }
                        }
                    }
                }
            }
        }
        return forcedRet ?? ret;
    }

    public static InventoryType[] ValidInventories =
    [
        InventoryType.EquippedItems,
        InventoryType.ArmoryOffHand,
        InventoryType.ArmoryHead,
        InventoryType.ArmoryBody,
        InventoryType.ArmoryHands,
        InventoryType.ArmoryWaist,
        InventoryType.ArmoryLegs,
        InventoryType.ArmoryFeets,
        InventoryType.ArmoryEar,
        InventoryType.ArmoryNeck,
        InventoryType.ArmoryWrist,
        InventoryType.ArmoryRings,
        InventoryType.ArmoryMainHand,
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
    ];

    public static InventoryType[] ValidInventoriesArmory =
    [
        InventoryType.EquippedItems,
        InventoryType.ArmoryOffHand,
        InventoryType.ArmoryHead,
        InventoryType.ArmoryBody,
        InventoryType.ArmoryHands,
        InventoryType.ArmoryWaist,
        InventoryType.ArmoryLegs,
        InventoryType.ArmoryFeets,
        InventoryType.ArmoryEar,
        InventoryType.ArmoryNeck,
        InventoryType.ArmoryWrist,
        InventoryType.ArmoryRings,
        InventoryType.ArmoryMainHand,
    ];

    public static float GetBaseParamPrio(this Job job, BaseParamEnum param)
    {
        {
            if(C.Priorities.TryGetValue(job, out var result) && result.TryGetValue(param, out var result2))
            {
                return result2;
            }
        }
        {
            if(GetBaseParamPrioForJob(job).TryGetValue(param, out var result2))
            {
                return result2;
            }
        }
        return 0;
    }

    public static Dictionary<BaseParamEnum, float> GetBaseParamPrioForJob(Job job)
    {
        if(Svc.Data.GetExcelSheet<ClassJob>().TryGetRow((uint)job, out var data))
        {
            if(data.ClassJobCategory.RowId == 33)
            {
                //crafters
                return GetDefaultBParamDict([BaseParamEnum.CP, BaseParamEnum.Control, BaseParamEnum.Craftsmanship, BaseParamEnum.Control]);
            }
            if(data.ClassJobCategory.RowId == 32) //gatherers
            {
                return GetDefaultBParamDict([BaseParamEnum.GP, BaseParamEnum.Gathering, BaseParamEnum.Perception]);
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(73).IsJobInCategory(job)) //healers
            {
                return GetDefaultBParamDict([BaseParamEnum.Mind, BaseParamEnum.Piety, BaseParamEnum.SpellSpeed, BaseParamEnum.Determination, BaseParamEnum.CriticalHit, BaseParamEnum.SpellSpeed]);
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(113).IsJobInCategory(job)) //tanks
            {
                return GetDefaultBParamDict([BaseParamEnum.Strength, BaseParamEnum.Determination, BaseParamEnum.Tenacity, BaseParamEnum.DirectHitRate, BaseParamEnum.CriticalHit, BaseParamEnum.SkillSpeed]);
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(84).IsJobInCategory(job)) //strength dps
            {
                return GetDefaultBParamDict([BaseParamEnum.Strength, BaseParamEnum.Determination, BaseParamEnum.DirectHitRate, BaseParamEnum.CriticalHit, BaseParamEnum.SkillSpeed]);
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(105).IsJobInCategory(job)) //dexterity dps
            {
                return GetDefaultBParamDict([BaseParamEnum.Dexterity, BaseParamEnum.Determination, BaseParamEnum.DirectHitRate, BaseParamEnum.CriticalHit, BaseParamEnum.SkillSpeed]);
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(63).IsJobInCategory(job)) //magical dps
            {
                return GetDefaultBParamDict([BaseParamEnum.Intelligence, BaseParamEnum.Determination, BaseParamEnum.DirectHitRate, BaseParamEnum.CriticalHit, BaseParamEnum.SpellSpeed]);
            }
        }
        return [];
    }

    public static JobRole GetRole(this Job job)
    {
        if(Svc.Data.GetExcelSheet<ClassJob>().TryGetRow((uint)job, out var data))
        {
            if(data.ClassJobCategory.RowId == 33)
            {
                //crafters
                return JobRole.Crafters;
            }
            if(data.ClassJobCategory.RowId == 32) //gatherers
            {
                return JobRole.Gatherers;
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(73).IsJobInCategory(job)) //healers
            {
                return JobRole.Healers;
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(113).IsJobInCategory(job)) //tanks
            {
                return JobRole.Tanks;
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(114).IsJobInCategory(job)) //melee dps
            {
                return JobRole.Melee;
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(66).IsJobInCategory(job)) //ranged dps
            {
                return JobRole.PhysicalRanged;
            }
            if(Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(63).IsJobInCategory(job)) //magical dps
            {
                return JobRole.MagicalRanged;
            }
        }
        return JobRole.Other;
    }

    public static Dictionary<BaseParamEnum, float> GetDefaultBParamDict(BaseParamEnum[] defaultParams)
    {
        var ret = new Dictionary<BaseParamEnum, float>();
        foreach(var x in CheckedBaseParams)
        {
            ret[x] = defaultParams.Contains(x) ? 1 : 0;
        }
        return ret;
    }

    public static BaseParamEnum[] CheckedBaseParams = 
    [
        BaseParamEnum.Dexterity,
        BaseParamEnum.Strength,
        BaseParamEnum.Mind,
        BaseParamEnum.Intelligence,
        BaseParamEnum.Piety,
        BaseParamEnum.GP,
        BaseParamEnum.CP,
        BaseParamEnum.Tenacity,
        BaseParamEnum.DirectHitRate,
        BaseParamEnum.CriticalHit,
        BaseParamEnum.Determination,
        BaseParamEnum.SkillSpeed,
        BaseParamEnum.SpellSpeed,
        BaseParamEnum.Craftsmanship,
        BaseParamEnum.Control,
        BaseParamEnum.Gathering,
        BaseParamEnum.Perception,
    ];
}
