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

namespace Lifestream
{
    internal unsafe class Memory : IDisposable
    {
        delegate long AddonAreaMap_ReceiveEvent(long a1, ushort a2, uint a3, long a4, long a5);
        [Signature("48 89 5C 24 ?? 57 48 83 EC 20 0F B7 C2 49 8B F9 83 C0 FD 48 8B D9 83 F8 20", DetourName = nameof(AddonAreaMap_ReceiveEventDetour), Fallibility = Fallibility.Fallible)]
        Hook<AddonAreaMap_ReceiveEvent> AddonAreaMap_ReceiveEventHook = null!;
        bool IsLeftMouseHeld = false;

        internal Memory()
        {
            SignatureHelper.Initialise(this);
            AddonAreaMap_ReceiveEventHook.Enable();
        }

        long AddonAreaMap_ReceiveEventDetour(long a1, ushort a2, uint a3, long a4, long a5)
        {
            //DuoLog.Information($"{a1}, {a2}, {a3}, {a4}, {a5}");
            try
            {
                if (P.ActiveAetheryte != null && Util.CanUseOverlay())
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
        }
    }
}
