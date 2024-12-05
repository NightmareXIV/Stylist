using ECommons.ExcelServices;
using Lumina.Excel.Sheets;

namespace Stylist;
public static class Utils
{
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
