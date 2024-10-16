using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons.EzHookManager;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Enums;
using Lifestream.Tasks.SameWorld;
using PInvoke;
using System.Windows.Forms;

namespace Lifestream.Game;

internal unsafe class Memory : IDisposable
{
    private delegate long AddonAreaMap_ReceiveEvent(long a1, ushort a2, uint a3, long a4, long a5);
    [Signature("40 55 56 57 48 8B EC 48 83 EC 70 0F B7 C2", DetourName = nameof(AddonAreaMap_ReceiveEventDetour), Fallibility = Fallibility.Fallible)]
    private Hook<AddonAreaMap_ReceiveEvent> AddonAreaMap_ReceiveEventHook = null!;
    private bool IsLeftMouseHeld = false;

    internal delegate void AddonDKTWorldList_ReceiveEventDelegate(nint a1, short a2, nint a3, AtkEvent* a4, InputData* a5);
    [Signature("40 53 48 83 EC 20 F6 81 ?? ?? ?? ?? ?? 49 8B D9 41 8B C0", DetourName = nameof(AddonDKTWorldList_ReceiveEventDetour))]
    internal Hook<AddonDKTWorldList_ReceiveEventDelegate> AddonDKTWorldList_ReceiveEventHook;

    internal delegate void AtkComponentTreeList_vf31(nint a1, uint a2, byte a3);
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 41 0F B6 F0", DetourName = nameof(AtkComponentTreeList_vf31Detour))]
    internal Hook<AtkComponentTreeList_vf31> AtkComponentTreeList_vf31Hook;

    [Signature("4C 8D 0D ?? ?? ?? ?? 4C 8B 11 48 8B D9", ScanType = ScanType.StaticAddress)]
    internal int* MaxInstances;

    internal delegate byte OpenPartyFinderInfoDelegate(void* agentLfg, ulong contentId);
    [EzHook("40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 84 C0 74 07 C6 83 ?? ?? ?? ?? ?? 48 83 C4 20 5B C3 CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 40 53", false)]
    internal EzHook<OpenPartyFinderInfoDelegate> OpenPartyFinderInfoHook;

    internal byte OpenPartyFinderInfoDetour(void* agentLfg, ulong contentId)
    {
        PluginLog.Information($"{((nint)agentLfg):X16}, {contentId:X16}");
        return OpenPartyFinderInfoHook.Original(agentLfg, contentId);
    }

    private void AtkComponentTreeList_vf31Detour(nint a1, uint a2, byte a3)
    {
        PluginLog.Debug($"AtkComponentTreeList_vf31Detour: {a1:X16}, {a2}, {a3}");
        AtkComponentTreeList_vf31Hook.Original(a1, a2, a3);
    }

    private void AddonDKTWorldList_ReceiveEventDetour(nint a1, short a2, nint a3, AtkEvent* a4, InputData* a5)
    {
        PluginLog.Debug($"AddonDKTWorldCheck_ReceiveEventDetour: {a1:X16}, {a2}, {a3:X16}, {(nint)a4:X16}, {(nint)a5:X16}");
        PluginLog.Debug($"  Event: {(nint)a4->Node:X16}, {(nint)a4->Target:X16}, {(nint)a4->Listener:X16}, {a4->Param}, {(nint)a4->NextEvent:X16}, {a4->Type}, {a4->Unk29}, {a4->Flags}");
        PluginLog.Debug($"  Data: {(nint)a5->unk_8:X16}({*a5->unk_8:X16}/{*a5->unk_8:X16}), [{a5->unk_8s->unk_4}/{a5->unk_8s->SelectedItem}] {a5->unk_16}, {a5->unk_24} | "); //{a5->RawDumpSpan.ToArray().Print()}
        //var span = new Span<byte>((void*)*a5->unk_8, 0x40).ToArray().Select(x => $"{x:X2}");
        //PluginLog.Debug($"  Data 2, {a5->unk_8s->unk_4}, {MemoryHelper.ReadRaw((nint)a5->unk_8s->CategorySelection, 4).Print(",")},  :{string.Join(" ", span)}");
        AddonDKTWorldList_ReceiveEventHook.Original(a1, a2, a3, a4, a5);
    }

    internal void ConstructEvent(AtkUnitBase* addon, int category, int which, int nodeIndex, int itemToSelect, int itemToHighlight)
    {
        if(itemToSelect == 0) throw new Exception("Enumeration starts with 1");
        var Event = stackalloc AtkEvent[1]
        {
            new AtkEvent()
            {
                Node = null,
                Target = (AtkEventTarget*)addon->UldManager.NodeList[nodeIndex],
                Listener = &addon->UldManager.NodeList[nodeIndex]->GetAsAtkComponentNode()->Component->AtkEventListener,
                Param = 1,
                NextEvent = null,
                Type = AtkEventType.ListItemToggle,
                Unk29 = 0,
                Flags = 0,
            }
        };
        var Unk = stackalloc UnknownStruct[1]
        {
            new()
            {
                unk_4 = 1,
                SelectedItem = itemToSelect - 1 + (category << 8)
            }
        };
        var ptr = stackalloc nint[1]
        {
            (nint)Unk
        };
        var Data = stackalloc InputData[1]
        {
            new InputData()
            {
                unk_8 = ptr,
                unk_16 = itemToSelect,
                unk_24 = 0,
            }
        };
        AddonDKTWorldList_ReceiveEventDetour((nint)addon, 35, which, Event, Data);
        AtkComponentTreeList_vf31Detour((nint)addon->UldManager.NodeList[nodeIndex]->GetAsAtkComponentList(), (uint)itemToHighlight, 0);
    }

    internal Memory()
    {
        SignatureHelper.Initialise(this);
        EzSignatureHelper.Initialize(this);
        AddonAreaMap_ReceiveEventHook.Enable();
        //AddonDKTWorldList_ReceiveEventHook.Enable();
    }

    private long AddonAreaMap_ReceiveEventDetour(long a1, ushort a2, uint a3, long a4, long a5)
    {
        //DuoLog.Information($"{a1}, {a2}, {a3}, {a4}, {a5}");
        try
        {
            if((P.ActiveAetheryte != null || P.ResidentialAethernet.ActiveAetheryte != null) && Utils.CanUseAetheryte() != AetheryteUseState.None)
            {
                if(a2 == 3)
                {
                    IsLeftMouseHeld = Bitmask.IsBitSet(User32.GetKeyState((int)Keys.LButton), 15);
                }
                if(a2 == 4 && IsLeftMouseHeld)
                {
                    IsLeftMouseHeld = false;
                    if(!Bitmask.IsBitSet(User32.GetKeyState((int)Keys.ControlKey), 15) && !Bitmask.IsBitSet(User32.GetKeyState((int)Keys.LControlKey), 15) && !Bitmask.IsBitSet(User32.GetKeyState((int)Keys.RControlKey), 15))
                    {
                        if(TryGetAddonByName<AtkUnitBase>("Tooltip", out var addon) && IsAddonReady(addon) && addon->IsVisible)
                        {
                            var node = addon->UldManager.NodeList[2]->GetAsAtkTextNode();
                            var text = MemoryHelper.ReadSeString(&node->NodeText).ExtractText();
                            if(P.ActiveAetheryte != null)
                            {
                                var master = Utils.GetMaster();
                                foreach(var x in P.DataStore.Aetherytes[master])
                                {
                                    if(x.Name == text)
                                    {
                                        TaskAethernetTeleport.Enqueue(x);
                                        break;
                                    }
                                }
                            }
                            if(P.ResidentialAethernet.ActiveAetheryte != null)
                            {
                                var zone = P.ResidentialAethernet.ZoneInfo.SafeSelect(Svc.ClientState.TerritoryType);
                                if(zone != null)
                                {
                                    foreach(var x in zone.Aetherytes)
                                    {
                                        if(x.Name == text)
                                        {
                                            TaskAethernetTeleport.Enqueue(x.Name);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        return AddonAreaMap_ReceiveEventHook!.Original(a1, a2, a3, a4, a5);
    }

    public void Dispose()
    {
        AddonAreaMap_ReceiveEventHook?.Disable();
        AddonAreaMap_ReceiveEventHook?.Dispose();
        AddonDKTWorldList_ReceiveEventHook?.Disable();
        AddonDKTWorldList_ReceiveEventHook?.Dispose();
        AtkComponentTreeList_vf31Hook?.Disable();
        AtkComponentTreeList_vf31Hook?.Dispose();
    }
}
