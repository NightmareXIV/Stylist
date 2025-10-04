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
    /// <param name="shouldEquip">Whether to equip specified gearset. Setting it to true will always equip it, no matter if it was updated or not. Setting it to false will never equip it. Setting it to null will use player's preferences.</param>
    [EzIPC]
    public void UpdateGearsetIfNeededEx(int gearsetIndex, bool? moveItemsFromInventory, bool? shouldEquip)
    {
        moveItemsFromInventory ??= C.UseInventory;
        Utils.UpdateGearsetIfNeeded(gearsetIndex, moveItemsFromInventory.Value, shouldEquip);
    }

    /// <inheritdoc cref="UpdateGearsetIfNeededEx(int, bool?, bool?)"/>
    [EzIPC]
    [Obsolete("Use UpdateGearsetIfNeededEx if possible")]
    public void UpdateGearsetIfNeeded(int gearsetIndex, bool? moveItemsFromInventory)
    {
        this.UpdateGearsetIfNeededEx(gearsetIndex, moveItemsFromInventory, null);
    }

    /// <summary>
    /// Updates current gearset, if present
    /// </summary>
    /// <param name="moveItemsFromInventory">null - respect configuration choice</param>
    /// <param name="shouldEquip">Whether to equip specified gearset. Setting it to true will always equip it, no matter if it was updated or not. Setting it to false will never equip it. Setting it to null will use player's preferences.</param>
    [EzIPC]
    public void UpdateCurrentGearsetEx(bool? moveItemsFromInventory, bool? shouldEquip)
    {
        moveItemsFromInventory ??= C.UseInventory;
        var index = RaptureGearsetModule.Instance()->CurrentGearsetIndex;
        if(index != 255 && RaptureGearsetModule.Instance()->GetGearset(index) != null && RaptureGearsetModule.Instance()->IsValidGearset(index))
        {
            Utils.UpdateGearsetIfNeeded(index, moveItemsFromInventory.Value, shouldEquip);
        }
    }

    /// <inheritdoc cref="UpdateCurrentGearsetEx(bool?, bool?)"/>
    [EzIPC]
    [Obsolete("Use UpdateCurrentGearsetEx if possible")]
    public void UpdateCurrentGearset(bool? moveItemsFromInventory)
    {
        this.UpdateCurrentGearsetEx(moveItemsFromInventory, null);
    }

    [EzIPC]
    public bool IsBusy()
    {
        return P.TaskManager.IsBusy;
    }
}