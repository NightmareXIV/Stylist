using ECommons.EzHookManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist.Services;
public sealed class Memory
{
    public static int ClassInfoCnt = 0;
    delegate nint ProcessPacketUpdateClassInfoInnerDelegate(nint a1, nint a2);
    [EzHook("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 48 8D 0D ?? ?? ?? ?? 33 D2")]
    EzHook<ProcessPacketUpdateClassInfoInnerDelegate> ProcessPacketUpdateClassInfoInnerHook;

    nint ProcessPacketUpdateClassInfoInnerDetour(nint a1, nint a2)
    {
        ClassInfoCnt++;
        return ProcessPacketUpdateClassInfoInnerHook.Original(a1, a2);
    }

    private Memory()
    {
        EzSignatureHelper.Initialize(this);
    }
}