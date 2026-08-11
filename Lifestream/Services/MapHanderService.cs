using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Tasks.SameWorld;
using System.Windows.Forms;
using Dalamud.Game.Addon.Events;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lifestream.Systems;
using Lifestream.Systems.Legacy;
using FXWindows = TerraFX.Interop.Windows.Windows;

namespace Lifestream.Services;
public unsafe class MapHanderService : IDisposable
{
    private MapHanderService()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "AreaMap", OnMapReceivedEvent);
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, "AreaMap", OnMapReceivedEvent);
    }

    private void OnMapReceivedEvent(AddonEvent type, AddonArgs args)
    {
        if (!C.UseMapTeleport)
        {
            return;
        }

        var addon = (AddonAreaMap*)args.Addon.Address;
        if (args is not AddonReceiveEventArgs evt ||
            !addon->AtkUnitBase.IsReady() ||
            Utils.IsBusy())
        {
            return;
        }

        var atkEventData = (AtkEventData*)evt.AtkEventData;
        var isLeftClicked = atkEventData->MouseData.ButtonId == 0;
        var isGamePadClick = atkEventData->InputData.State == InputState.Up;
        var isGamePadInput = evt.AtkEventType == AddonEventType.InputBaseInputReceived;
        var isMouseUp = evt.AtkEventType == AddonEventType.MouseUp;

        if ((!isMouseUp || !isLeftClicked) && (!isGamePadInput || !isGamePadClick))
        {
            return;
        }

        if (Bitmask.IsBitSet(FXWindows.GetKeyState((int)Keys.ControlKey), 15) ||
            Bitmask.IsBitSet(FXWindows.GetKeyState((int)Keys.LControlKey), 15) ||
            Bitmask.IsBitSet(FXWindows.GetKeyState((int)Keys.RControlKey), 15))
        {
            return;
        }

        var agentMap = AgentMap.Instance();
        if  (agentMap == null)
        {
            return;
        }

        var activeId = 0u;
        IAetheryte targetAetheryte = null;

        if (P.ActiveAetheryte != null)
        {
            var master = Utils.GetMaster();
            if (S.Data.DataStore.Aetherytes.TryGetValue(master, out var aetheryteList))
            {
                var validAetherytes = aetheryteList
                    .Where(x => x.TerritoryType == agentMap->SelectedTerritoryId && !x.Invisible)
                    .ToList();

                // Include the master one to allow to teleport to it via aethernet network
                if (master.TerritoryType == agentMap->SelectedTerritoryId && !master.Invisible)
                {
                    validAetherytes.Insert(0, master);
                }

                if (TryGetNearbyAetheryte(validAetherytes, addon->HoveredCoords, out targetAetheryte))
                {
                    activeId = P.ActiveAetheryte!.Value.ID;
                }
            }
        }

        if (targetAetheryte == null && S.Data.ResidentialAethernet.ActiveAetheryte != null)
        {
            var zone = S.Data.ResidentialAethernet.ZoneInfo.SafeSelect(P.Territory);
            if (zone != null && TryGetNearbyAetheryte(zone.Aetherytes, addon->HoveredCoords, out targetAetheryte))
            {
                activeId = S.Data.ResidentialAethernet.ActiveAetheryte.Value.ID;
            }
        }

        if (targetAetheryte == null && S.Data.CustomAethernet.ActiveAetheryte != null)
        {
            var zone = S.Data.CustomAethernet.ZoneInfo.SafeSelect(P.Territory);
            if (zone != null && TryGetNearbyAetheryte(zone.Aetherytes, addon->HoveredCoords, out targetAetheryte))
            {
                activeId = S.Data.CustomAethernet.ActiveAetheryte.Value.ID;
            }
        }

        if (activeId != 0 && targetAetheryte != null)
        {
            if (activeId == targetAetheryte.ID)
            {
                Notify.Error("You are already here!");
            }
            else
            {
                if (targetAetheryte is TinyAetheryte tinyAetheryte)
                {
                    if (tinyAetheryte.IsAetheryte)
                    {
                        // This releases the mouse from the mouse down without processing anything else (ignoring the aetheryte click).
                        // See case AtkEventType_MouseUp with the called function in Client::UI::AddonAreaMap_ReceiveEvent.
                        // The value only has to be higher than 1, using an unused ButtonId in this context to be safe.
                        atkEventData->MouseData.ButtonId = 50;
                    }

                    TaskAethernetTeleport.Enqueue(tinyAetheryte);
                }
                else
                {
                    TaskAethernetTeleport.Enqueue(targetAetheryte.Name);
                }
            }

            return;
        }

        if (!C.DisableMapClickOtherTerritory)
        {
            var masterEntry = S.Data.DataStore.Aetherytes.FirstOrNull(x => x.Value.Any(y => y.TerritoryType == agentMap->SelectedTerritoryId));
            if (masterEntry is var (master, aetherytes))
            {
                var validAetherytes = aetherytes.Where(x => x.TerritoryType == agentMap->SelectedTerritoryId && !x.Invisible);
                if (TryGetNearbyAetheryte(validAetherytes, addon->HoveredCoords, out var nearbyAetheryte))
                {
                    TaskAetheryteAethernetTeleport.Enqueue(master.ID, nearbyAetheryte.ID);
                }
            }
        }
    }

    private bool TryGetNearbyAetheryte<T>(IEnumerable<T> aetherytes, Vector2 hoveredCoords, out IAetheryte nearbyAetheryte) where T : IAetheryte
    {
        nearbyAetheryte = null;

        var agentMap = AgentMap.Instance();
        if (agentMap == null)
        {
            return false;
        }

        var closest = aetherytes?.Select(x => new
            {
                Aetheryte = x,
                Distance = Vector2.Distance(MapUtil.WorldToMap(x.Position, -agentMap->SelectedOffsetX, -agentMap->SelectedOffsetY, (uint)agentMap->SelectedMapSizeFactor), hoveredCoords)
            })
            .MinBy(x => x.Distance);

        if (closest != null && closest.Distance <= C.MaximumMapClickDistance * 100 / agentMap->SelectedMapSizeFactor)
        {
            nearbyAetheryte = closest.Aetheryte;
            return true;
        }

        return false;
    }
}
