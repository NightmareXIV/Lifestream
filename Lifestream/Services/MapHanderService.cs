using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using ECommons.MathHelpers;
using ECommons.UIHelpers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Enums;
using Lifestream.Systems.Residential;
using Lifestream.Tasks.SameWorld;
using Lumina.Excel.Sheets;
using PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lifestream.Services;
public unsafe class MapHanderService : IDisposable
{
    private MapHanderService()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "AreaMap", OnMapReceivedEvent);
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "AreaMap", OnMapReceivedEvent);
    }

    private void OnMapReceivedEvent(AddonEvent type, AddonArgs args)
    {
        if(args is AddonReceiveEventArgs evt && TryGetAddonByName<AddonAreaMap>("AreaMap", out var addon) && addon->AtkUnitBase.IsReady() && !Utils.IsBusy())
        {
            /*var atkEvent = (AtkEvent*)evt.AtkEvent;
            var data = MemoryHelper.ReadRaw(evt.Data, 40);
            PluginLog.Information($"""
                EventParam: {evt.EventParam}
                AtkEventType: {evt.AtkEventType}
                atkEvent->Param: {atkEvent->Param}
                atkEvent->Node->NodeId: {(atkEvent->Node == null?"-":atkEvent->Node->NodeId)}
                atkEvent->State: {atkEvent->State.StateFlags}
                data: {data.ToHexString()}
                CursorTarget: {(addon->CursorTarget == null?"-": addon->CursorTarget->NodeId)}
                """);*/
            var isLeftClicked = *(byte*)(evt.Data + 6) == 0;
            if(evt.AtkEventType == (int)AtkEventType.MouseUp)
            {
                if(isLeftClicked)
                {
                    if(!Bitmask.IsBitSet(User32.GetKeyState((int)Keys.ControlKey), 15) && !Bitmask.IsBitSet(User32.GetKeyState((int)Keys.LControlKey), 15) && !Bitmask.IsBitSet(User32.GetKeyState((int)Keys.RControlKey), 15))
                    {
                        if(TryGetAddonByName<AtkUnitBase>("Tooltip", out var addonTooltip) && IsAddonReady(addonTooltip) && addonTooltip->IsVisible)
                        {
                            var node = addonTooltip->UldManager.NodeList[2]->GetAsAtkTextNode();
                            var text = GenericHelpers.ReadSeString(&node->NodeText).GetText();
                            if(P.ActiveAetheryte != null)
                            {
                                var master = Utils.GetMaster();
                                foreach(var x in S.Data.DataStore.Aetherytes[master])
                                {
                                    if(x.Name == text)
                                    {
                                        if(P.ActiveAetheryte.Value.ID == x.ID)
                                        {
                                            Notify.Error("You are already here!");
                                        }
                                        else
                                        {
                                            TaskAethernetTeleport.Enqueue(x);
                                        }
                                        return;
                                    }
                                }
                            }
                            if(S.Data.ResidentialAethernet.ActiveAetheryte != null)
                            {
                                var zone = S.Data.ResidentialAethernet.ZoneInfo.SafeSelect(P.Territory);
                                if(zone != null)
                                {
                                    foreach(var x in zone.Aetherytes)
                                    {
                                        if(x.Name == text)
                                        {
                                            if(S.Data.ResidentialAethernet.ActiveAetheryte.Value.ID == x.ID)
                                            {
                                                Notify.Error("You are already here!");
                                            }
                                            else
                                            {
                                                TaskAethernetTeleport.Enqueue(x.Name);
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                            if(S.Data.CustomAethernet.ActiveAetheryte != null)
                            {
                                var zone = S.Data.CustomAethernet.ZoneInfo.SafeSelect(P.Territory);
                                if(zone != null)
                                {
                                    foreach(var x in zone.Aetherytes)
                                    {
                                        if(x.Name.StartsWith(text))
                                        {
                                            if(S.Data.CustomAethernet.ActiveAetheryte.Value.ID == x.ID)
                                            {
                                                Notify.Error("You are already here!");
                                            }
                                            else
                                            {
                                                TaskAethernetTeleport.Enqueue(x.Name);
                                            }
                                            return;
                                        }
                                    }
                                    if(zone.GenericAetheryteNames.Contains(text))
                                    {
                                        var target = zone.Aetherytes.MinBy(x => Vector2.Distance(x.MapPosition.Value, addon->HoveredCoords));
                                        TaskAethernetTeleport.Enqueue(target.Name);
                                    }
                                }
                            }
                            if(!C.DisableMapClickOtherTerritory)
                            {
                                foreach(var x in S.Data.DataStore.Aetherytes)
                                {
                                    foreach(var a in x.Value)
                                    {
                                        if(a.Name == text)
                                        {
                                            TaskAetheryteAethernetTeleport.Enqueue(x.Key.ID, a.ID);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
