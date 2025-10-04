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

    public static InventoryDescriptor? FindGearsetItemInInventory(RaptureGearsetModule.GearsetItem gearsetItem, InventoryContainer* cont)
    {
        for(int i = 0; i < cont->Size; i++)
        {
            var item = cont->GetInventorySlot(i);
            if(
                item->GetItemId() == gearsetItem.ItemId
                && item->Stains.SequenceEqual([gearsetItem.Stain0Id, gearsetItem.Stain1Id])
                && item->Materia.SequenceEqual(gearsetItem.Materia)
                && item->MateriaGrades.SequenceEqual(gearsetItem.MateriaGrades)
                && gearsetItem.GlamourId == item->GlamourId
                )
            {
                return new(cont->Type, i);
            }
        }
        return null;
    }

    public static void UpdateGearsetIfNeeded(int index, bool includeInventory = true, bool? shouldEquip = null)
    {
        var r = RaptureGearsetModule.Instance();
        var isCurrent = r->CurrentGearsetIndex == index;
        if(C.BlacklistedGearsets.SafeSelect(Player.CID)?.Contains((int)index) == true)
        {
            PluginLog.Debug($"Gearset {index + 1} blacklisted");
            return;
        }
        List<RaptureGearsetModule.GearsetItem> itemsToUnmove = [];
        if(index < r->NumGearsets && r->IsValidGearset(index))
        {
            var entry = r->Entries.GetPointer(index);
            InventoryDescriptor? ring = null;
            for(int q = 0; q < entry->Items.Length && q < Utils.GearsetSlotMap.Length; q++)
            {
                var gsItem = entry->GetItem((RaptureGearsetModule.GearsetItemIndex)q);
                var candidate = Utils.GetBestItemForJob((Job)entry->ClassJob, Utils.GearsetSlotMap[q], true, q == 12 ? [ring] : null, includeInventory);
                if(q == 11) ring = candidate;
                if(candidate != null)
                {
                    if(candidate.Value.GetSlot().GetItemId() == gsItem.ItemId)
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
                            itemsToUnmove.Add(gsItem);
                        }
                        PluginLog.Debug($"Setting item for slot {q}");
                        shouldEquip ??= isCurrent && C.Reequip;
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

        if(shouldEquip == true)
        {
            PluginLog.Information($"Re-equipping gearset {index} once all items moved to armory chest");
            var cnt = Memory.ClassInfoCnt;
            P.TaskManager.Enqueue(() =>
            {
                if(S.ItemMover.ItemsToMove.Count > 0) return false;
                if(!Player.Interactable) return false;
                if(!Svc.ClientState.IsLoggedIn) return null;
                RaptureGearsetModule.Instance()->EquipGearset(index);
                return true;
            }, new(showDebug: true));
            P.TaskManager.Enqueue(() => Memory.ClassInfoCnt != cnt, new(showDebug: true, abortOnTimeout: false, timeLimitMS: 5000));
            P.TaskManager.Enqueue(() =>
            {
                return !Utils.CheckForUpdateNeeded(index, includeInventory);
            }, new(showDebug: true));
            if(C.UnmoveItems)
            {
                P.TaskManager.Enqueue(() => S.ItemMover.ItemsToUnmove = itemsToUnmove);
                P.TaskManager.Enqueue(() => S.ItemMover.ItemsToUnmove.Count == 0, new(showDebug: true));
            }
        }
    }

    public static bool CheckForUpdateNeeded(int index, bool includeInventory = true)
    {
        var r = RaptureGearsetModule.Instance();
        var isCurrent = r->CurrentGearsetIndex == index;
        if(C.BlacklistedGearsets.SafeSelect(Player.CID)?.Contains((int)index) == true)
        {
            PluginLog.Debug($"Gearset {index + 1} blacklisted");
            return false;
        }
        if(index < r->NumGearsets && r->IsValidGearset(index))
        {
            var normal = r->Entries[index];
            var ringReversed = normal;
            (normal.Items[11], normal.Items[12]) = (normal.Items[12], normal.Items[11]);

            int passes = 0;

            foreach(var entry in (RaptureGearsetModule.GearsetEntry[])[normal, ringReversed])
            {
                InventoryDescriptor? ring = null;
                for(int q = 0; q < entry.Items.Length && q < Utils.GearsetSlotMap.Length; q++)
                {
                    var gsItem = entry.GetItem((RaptureGearsetModule.GearsetItemIndex)q);
                    var candidate = Utils.GetBestItemForJob((Job)entry.ClassJob, Utils.GearsetSlotMap[q], true, q == 12 ? [ring] : null, includeInventory);
                    if(q == 11) ring = candidate;
                    if(candidate != null)
                    {
                        if(candidate.Value.GetSlot().GetItemId() == gsItem.ItemId)
                        {
                            PluginLog.Debug($"Skipping existing item for slot {q}");
                        }
                        else
                        {
                            passes++;
                            break;
                        }
                    }
                }
            }
            PluginLog.Debug($"Versions passed: {passes}");
            return passes == 2;
        }
        return false;
    }

    public static InventoryDescriptor? GetBestItemForJob(this Job job, EquipSlotCategoryEnum[] slot, bool restrictLevel = true, InventoryDescriptor?[] ignore = null, bool includeInventory = true)
    {
        var ignoreIlvl = job.IsDol() || job.IsDoh();
        PluginLog.Verbose($"GetBestItemForJob {job}, slots {slot.Print()}, restrictLevel={restrictLevel}, ignore={ignore?.Print()}");
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
                    var suitableJobs = GetSuitableJobsForItem(descriptor.Data.RowId);
                    if(suitableJobs.Count > 0 && !suitableJobs.Contains(job)) continue;
                    if(descriptor.Data.ValueNullable != null && slot.Contains((EquipSlotCategoryEnum)descriptor.Data.Value.EquipSlotCategory.RowId) && descriptor.Data.Value.ClassJobCategory.Value.IsJobInCategory(job))
                    {
                        PluginLog.Verbose($"Consider {ExcelItemHelper.GetName(item->GetItemId() % 1000000, true)} from {type} / {i}");
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
                            if(ret == null || (descriptor.Data.Value.LevelItem.RowId > ret.Value.Data.Value.LevelItem.RowId && !ignoreIlvl))
                            {
                                PluginLog.Verbose($" >>Accepted over {ret} (ilvl)");
                                ret = descriptor;
                            }
                            else if(ignoreIlvl || descriptor.Data.Value.LevelItem.RowId == ret.Value.Data.Value.LevelItem.RowId)
                            {
                                if(GetBaseParamPrioForJob(job).Sum(x => (float)item->GetStat(x.Key) * x.Value) > GetBaseParamPrioForJob(job).Sum(x => (float)ret.Value.GetSlot().GetStat(x.Key) * x.Value))
                                {
                                    PluginLog.Verbose($" >>Accepted over {ret} (stats)");
                                    ret = descriptor;
                                }
                            }
                        }
                    }
                }
            }
        }
        if(forcedRet != null && ret != null && forcedRet.Value.Data.Value.LevelItem.RowId >= ret.Value.Data.Value.LevelItem.RowId)
        {
            return forcedRet;
        }
        return ret;
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

    public static List<Job> GetSuitableJobsForItem(this uint itemId)
    {
        if(Svc.Data.GetExcelSheet<Item>().TryGetRow(itemId, out var item))
        {
            var ret = new List<Job>();
            if(item.GetStat(BaseParamEnum.Intelligence) > 0)
            {
                ret.AddRange(Enum.GetValues<Job>().Where(x => x.IsMagicalRangedDps()));
            }
            if(item.GetStat(BaseParamEnum.Mind) > 0)
            {
                ret.AddRange(Enum.GetValues<Job>().Where(x => x.IsHealer()));
            }
            if(item.GetStat(BaseParamEnum.Dexterity) > 0)
            {
                ret.AddRange(Enum.GetValues<Job>().Where(x => Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(105).IsJobInCategory(x)));
            }
            if(item.GetStat(BaseParamEnum.Strength) > 0)
            {
                ret.AddRange(Enum.GetValues<Job>().Where(x => Svc.Data.GetExcelSheet<ClassJobCategory>().GetRow(84).IsJobInCategory(x) || x.IsTank()));
            }
            if(item.GetStat(BaseParamEnum.Gathering) > 0 || item.GetStat(BaseParamEnum.Perception) > 0 || item.GetStat(BaseParamEnum.GP) > 0)
            {
                ret.AddRange(Enum.GetValues<Job>().Where(x => x.IsDol()));
            }
            if(item.GetStat(BaseParamEnum.Craftsmanship) > 0 || item.GetStat(BaseParamEnum.Control) > 0 || item.GetStat(BaseParamEnum.CP) > 0)
            {
                ret.AddRange(Enum.GetValues<Job>().Where(x => x.IsDoh()));
            }
            if(ret.Count == 0)
            {
                if(item.GetStat(BaseParamEnum.Tenacity) > 0)
                {
                    ret.AddRange(Enum.GetValues<Job>().Where(x => x.IsTank()));
                }
                //can not determine by main stat
                if(item.GetStat(BaseParamEnum.DirectHitRate) > 0 || item.GetStat(BaseParamEnum.CriticalHit) > 0)
                {
                    //crit and dh suitable for any dps class
                    ret.AddRange(Enum.GetValues<Job>().Where(x => x.IsCombat()));
                }
                if(item.GetStat(BaseParamEnum.Piety) > 0) 
                {
                    //for healers
                    ret.AddRange(Enum.GetValues<Job>().Where(x => x.IsHealer()));
                }
                if(item.GetStat(BaseParamEnum.SpellSpeed) > 0)
                {
                    ret.AddRange(Enum.GetValues<Job>().Where(x => x.IsDom()));
                }
                if(item.GetStat(BaseParamEnum.SkillSpeed) > 0)
                {
                    ret.AddRange(Enum.GetValues<Job>().Where(x => x.IsDow()));
                }
            }
            return ret;
        }
        return [];
    }
}
