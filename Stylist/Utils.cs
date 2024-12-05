using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace Stylist;
public static unsafe class Utils
{
    public static readonly EquipSlotCategoryEnum[] EquipSlots = Enum.GetValues<EquipSlotCategoryEnum>().Where(x => (int)x <= 13).ToArray();

    public static InventoryDescriptor? GetBestItemForJob(this Job job, EquipSlotCategoryEnum slot, bool restrictLevel = true)
    {
        InventoryDescriptor? ret = null;
        var maxLvl = PlayerState.Instance()->ClassJobLevels[Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)job).ExpArrayIndex];
        foreach(var type in ValidInventories)
        {
            var inv = InventoryManager.Instance()->GetInventoryContainer(type);
            for(int i = 0; i < inv->GetSize(); i++)
            {
                var item = inv->GetInventorySlot(i);
                if(item->GetItemId() != 0)
                {
                    var descriptor = new InventoryDescriptor(type, i);
                    if(descriptor.Data.ValueNullable != null && descriptor.Data.Value.EquipSlotCategory.RowId == (uint)slot && descriptor.Data.Value.ClassJobCategory.Value.IsJobInCategory(job))
                    {
                        if(restrictLevel && descriptor.Data.Value.LevelEquip > maxLvl) continue;
                        if(ret == null || ret.Value.Data.Value.LevelItem.RowId > descriptor.Data.Value.LevelItem.RowId)
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
        return ret;
    }

    public static InventoryType[] ValidInventories = 
    [
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
        InventoryType.EquippedItems,
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
