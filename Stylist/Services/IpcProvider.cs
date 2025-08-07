using ECommons.EzIpcManager;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist.Services;
public unsafe sealed class IpcProvider
{
    private IpcProvider()
    {
        EzIPC.Init(this);
    }

    /// <summary>
    /// Updates gearset if there's anything to update and equips it.
    /// </summary>
    /// <param name="gearsetIndex">Gearset index to update</param>
    /// <param name="moveItemsFromInventory">null - respect configuration choice</param>
    [EzIPC]
    public void UpdateGearsetIfNeeded(int gearsetIndex, bool? moveItemsFromInventory)
    {
        moveItemsFromInventory ??= C.UseInventory;
        Utils.UpdateGearsetIfNeeded(gearsetIndex, moveItemsFromInventory.Value);
    }

    /// <summary>
    /// Updates current gearset, if present
    /// </summary>
    /// <param name="moveItemsFromInventory">null - respect configuration choice</param>
    [EzIPC]
    public void UpdateCurrentGearset(bool? moveItemsFromInventory)
    {
        moveItemsFromInventory ??= C.UseInventory;
        var index = RaptureGearsetModule.Instance()->CurrentGearsetIndex;
        if(index != 255 && RaptureGearsetModule.Instance()->GetGearset(index) != null && RaptureGearsetModule.Instance()->IsValidGearset(index))
        {
            Utils.UpdateGearsetIfNeeded(index, moveItemsFromInventory.Value);
        }
    }
}