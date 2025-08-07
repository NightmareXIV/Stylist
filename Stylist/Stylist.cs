using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.Automation.NeoTaskManager;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
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
    public List<DalamudLinkPayload> Links = [];
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
            new EzTerritoryChanged(OnTerritoryChanged);
            for(int i = 0; i < 101; i++)
            {
                Links.Add(Svc.Chat.AddChatLinkHandler(HandleLink));
            }
        });
    }

    private void OnTerritoryChanged(ushort t)
    {
        if(C.NotifyTerr.Contains(t))
        {
            CheckForSuggestions(false);
        }
    }

    public void CheckForSuggestions(bool printEmpty)
    {
        var candidates = new List<SeString>();
        var rgs = RaptureGearsetModule.Instance();
        for(int i = 0; i < rgs->Entries.Length; i++)
        {
            var entry = rgs->Entries[i];
            if(rgs->IsValidGearset(i))
            {
                if(Utils.CheckForUpdateNeeded(i, C.UseInventory))
                {
                    candidates.Add(new SeStringBuilder().Add(Links[i]).AddText(entry.NameString).Add(RawPayload.LinkTerminator).Build());
                }
            }
        }
        if(candidates.Count > 0)
        {
            var str = new SeStringBuilder()
                .AddUiForeground(42)
                .AddText("[Stylist] The following gearsets can be updated: ");
            for(int i = 0; i < candidates.Count; i++)
            {
                str = str.Append(candidates[i]);
                if(i < candidates.Count - 1)
                {
                    str = str.AddText(", ");
                }
                else
                {
                    str = str.AddText(". ");
                }
            }
            str = str.Add(Links[100]).AddText("Update all.").Add(RawPayload.LinkTerminator).AddUiForegroundOff();
            Svc.Chat.Print(new()
            {
                Message = str.Build()
            });
        }
        else
        {
            Svc.Chat.Print(new()
            {
                Message = new SeStringBuilder().AddUiForeground("[Stylist] All gearsets are up to date.", 42).Build()
            });
        }
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
        else if(arguments.EqualsIgnoreCaseAny("c", "config"))
        {
            EzConfigGui.Open();
        }
        else
        {
            CheckForSuggestions(true);
        }
        if(upd != 0)
        {
            DuoLog.Information($"Updated {upd} gearsets");
        }
    }

    public void HandleLink(Guid g, SeString text)
    {
        var index = this.Links.IndexOf(x => x.CommandId == g);
        PluginLog.Information($"Handling link {index}");
        if(index == 100)
        {
            var ret = UpdateGearsets(x => true);
            DuoLog.Information($"Updated {ret} gearsets");
        }
        else
        {
            var rgs = RaptureGearsetModule.Instance();
            var entry = rgs->Entries[(int)index];
            if(rgs->IsValidGearset((int)index))
            {
                Utils.UpdateGearsetIfNeeded((int)index, C.UseInventory);
            }
            DuoLog.Information($"Updated {entry.NameString}");
        }
    }

    public int UpdateGearsets(Predicate<RaptureGearsetModule.GearsetEntry> predicate)
    {
        var ret = 0;
        var rgs = RaptureGearsetModule.Instance();
        for(int i = 0; i < rgs->Entries.Length; i++)
        {
            if(C.BlacklistedGearsets.SafeSelect(Player.CID)?.Contains((int)i) == true)
            {
                PluginLog.Debug($"Gearset {i + 1} blacklisted");
                continue;
            }
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
        foreach(var x in Links)
        {
            Svc.Chat.RemoveChatLinkHandler(x.CommandId);
        }
        ECommonsMain.Dispose();
    }
}
