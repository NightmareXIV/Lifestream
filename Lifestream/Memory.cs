using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Dalamud.Utility;
using ECommons.MathHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PInvoke;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Memory;
using Lifestream.Tasks;
using System.Windows.Forms;
using Lifestream.UnmanagedStructs;

namespace Lifestream
{
    internal unsafe class Memory : IDisposable
    {
        delegate long AddonAreaMap_ReceiveEvent(long a1, ushort a2, uint a3, long a4, long a5);
        [Signature("48 89 5C 24 ?? 57 48 83 EC 20 0F B7 C2 49 8B F9 83 C0 FD 48 8B D9 83 F8 20", DetourName = nameof(AddonAreaMap_ReceiveEventDetour), Fallibility = Fallibility.Fallible)]
        Hook<AddonAreaMap_ReceiveEvent> AddonAreaMap_ReceiveEventHook = null!;
        bool IsLeftMouseHeld = false;

        internal delegate void AddonDKTWorldList_ReceiveEventDelegate(nint a1, short a2, nint a3, AtkEvent* a4, InputData* a5);
        [Signature("40 53 48 83 EC 20 F6 81 ?? ?? ?? ?? ?? 49 8B D9 41 8B C0", DetourName = nameof(AddonDKTWorldList_ReceiveEventDetour))]
        internal Hook<AddonDKTWorldList_ReceiveEventDelegate> AddonDKTWorldList_ReceiveEventHook;

        internal delegate void AtkComponentTreeList_vf31(nint a1, uint a2, byte a3);
        [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 41 0F B6 F0", DetourName = nameof(AtkComponentTreeList_vf31Detour))]
        internal Hook<AtkComponentTreeList_vf31> AtkComponentTreeList_vf31Hook;

        void AtkComponentTreeList_vf31Detour(nint a1, uint a2, byte a3)
        {
            PluginLog.Debug($"AtkComponentTreeList_vf31Detour: {a1:X16}, {a2}, {a3}");
            AtkComponentTreeList_vf31Hook.Original(a1, a2, a3);
        }

        void AddonDKTWorldList_ReceiveEventDetour(nint a1, short a2, nint a3, AtkEvent* a4, InputData* a5)
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
            if (itemToSelect == 0) throw new Exception("Enumeration starts with 1");
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
                    SelectedItem = (itemToSelect - 1) + (category << 8)
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
            AtkComponentTreeList_vf31Detour((nint)addon->UldManager.NodeList[nodeIndex]->GetAsAtkComponentList(), (uint)(itemToHighlight), 0);
        }

        internal Memory()
        {
            SignatureHelper.Initialise(this);
            AddonAreaMap_ReceiveEventHook.Enable();
            //AddonDKTWorldList_ReceiveEventHook.Enable();
        }

        long AddonAreaMap_ReceiveEventDetour(long a1, ushort a2, uint a3, long a4, long a5)
        {
            //DuoLog.Information($"{a1}, {a2}, {a3}, {a4}, {a5}");
            try
            {
                if (P.ActiveAetheryte != null && Util.CanUseAetheryte())
                {
                    var master = Util.GetMaster();
                    if (a2 == 3)
                    {
                        IsLeftMouseHeld = Bitmask.IsBitSet(User32.GetKeyState((int)Keys.LButton), 15);
                    }
                    if (a2 == 4 && IsLeftMouseHeld)
                    {
                        IsLeftMouseHeld = false;
                        if (!Bitmask.IsBitSet(User32.GetKeyState((int)Keys.ControlKey), 15) && !Bitmask.IsBitSet(User32.GetKeyState((int)Keys.LControlKey), 15) && !Bitmask.IsBitSet(User32.GetKeyState((int)Keys.RControlKey), 15))
                        {
                            if (TryGetAddonByName<AtkUnitBase>("Tooltip", out var addon) && IsAddonReady(addon) && addon->IsVisible)
                            {
                                var node = addon->UldManager.NodeList[2]->GetAsAtkTextNode();
                                var text = MemoryHelper.ReadSeString(&node->NodeText).ExtractText();
                                foreach(var x in P.DataStore.Aetherytes[master])
                                {
                                    if(x.Name == text)
                                    {
                                        TaskAethernetTeleport.Enqueue(x);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
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
}
