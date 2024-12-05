using Dalamud.Plugin;
using ECommons;
using ECommons.Automation.LegacyTaskManager;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.SimpleGui;
using ECommons.Singletons;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Stylist.Configuration;

namespace Stylist;

public unsafe class Stylist : IDalamudPlugin
{
    public static Stylist P;
    private Config Config;
    public static Config C => P.Config;
    public TaskManager TaskManager;
    public Stylist(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this);
        new TickScheduler(() =>
        {
            Config = EzConfig.Init<Config>();
            EzCmd.Add("/stylist", OnCommand, "Open the plugin's UI\n/stylist all|tank|healer|dps|crafter|gatherer|melee|ranged|magic - update certain role gearsets");
            SingletonServiceManager.Initialize(typeof(S));
            TaskManager = new();
        });
    }

    private void OnCommand(string command, string arguments)
    {
        var upd = 0;
        if(arguments.EqualsIgnoreCase("all"))
        {
            upd = UpdateGearsets(x => true);
        }
        else if(arguments.EqualsIgnoreCase("tank"))
        {
            upd = UpdateGearsets(x => ((Job)x.ClassJob).GetRole() == JobRole.Tanks);
        }
        else if(arguments.EqualsIgnoreCase("healer"))
        {
            upd = UpdateGearsets(x => ((Job)x.ClassJob).GetRole() == JobRole.Healers);
        }
        else if(arguments.EqualsIgnoreCase("ranged"))
        {
            upd = UpdateGearsets(x => ((Job)x.ClassJob).GetRole() == JobRole.PhysicalRanged);
        }
        else if(arguments.EqualsIgnoreCase("melee"))
        {
            upd = UpdateGearsets(x => ((Job)x.ClassJob).GetRole() == JobRole.Melee);
        }
        else if(arguments.EqualsIgnoreCase("magic"))
        {
            upd = UpdateGearsets(x => ((Job)x.ClassJob).GetRole() == JobRole.MagicalRanged);
        }
        else if(arguments.EqualsIgnoreCase("crafter"))
        {
            upd = UpdateGearsets(x => ((Job)x.ClassJob).GetRole() == JobRole.Crafters);
        }
        else if(arguments.EqualsIgnoreCase("gatherer"))
        {
            upd = UpdateGearsets(x => ((Job)x.ClassJob).GetRole() == JobRole.Gatherers);
        }
        else if(arguments.EqualsIgnoreCase("dps"))
        {
            upd = UpdateGearsets(x => ((Job)x.ClassJob).GetRole().EqualsAny(JobRole.Melee, JobRole.MagicalRanged, JobRole.PhysicalRanged));
        }
        else
        {
            EzConfigGui.Open();
        }
        if(upd != 0)
        {
            DuoLog.Information($"Updated {upd} gearsets");
        }
    }

    public int UpdateGearsets(Predicate<RaptureGearsetModule.GearsetEntry> predicate)
    {
        var ret = 0;
        var rgs = RaptureGearsetModule.Instance();
        for(int i = 0; i < rgs->Entries.Length; i++)
        {
            var entry = rgs->Entries[i];
            if(rgs->IsValidGearset(i) && predicate(entry))
            {
                PluginLog.Information($"Now updating gearset {i + 1}");
                ret++;
                Utils.UpdateGearsetIfNeeded(i, C.UseInventory);
            }
        }
        return ret;
    }

    public void Dispose()
    {
        ECommonsMain.Dispose();
    }
}
