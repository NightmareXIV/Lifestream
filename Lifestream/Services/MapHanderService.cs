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
    private float _lastMouseX;
    private float _lastMouseY;
    private bool _isMouseDown;
    private bool _hasDragged;

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

        var addonAreaMap = (AddonAreaMap*)args.Addon.Address;
        if (args is not AddonReceiveEventArgs evt ||
            !addonAreaMap->AtkUnitBase.IsReady())
        {
            return;
        }

        var atkEventData = (AtkEventData*)evt.AtkEventData;
        var isLeftClicked = atkEventData->MouseData.ButtonId == 0;
        var isGamePadClick = atkEventData->InputData.State == InputState.Up;
        var isGamePadInput = evt.AtkEventType == AddonEventType.InputBaseInputReceived;
        var isMouseUp = evt.AtkEventType == AddonEventType.MouseUp;
        var isMouseDown = evt.AtkEventType == AddonEventType.MouseDown;
        var isMouseMove = evt.AtkEventType == AddonEventType.MouseMove;
        var isClickCompleted = (isMouseUp && isLeftClicked) || (isGamePadInput && isGamePadClick);

        if (isMouseDown && isLeftClicked)
        {
            _lastMouseX = atkEventData->MouseData.PosX;
            _lastMouseY = atkEventData->MouseData.PosY;
            _isMouseDown = true;
            _hasDragged = false;
            return;
        }

        if (isMouseMove && _isMouseDown)
        {
            var uiScale = AtkUnitBase.GetGlobalUIScale();
            var deltaX = atkEventData->MouseData.PosX - _lastMouseX;
            var deltaY = atkEventData->MouseData.PosY - _lastMouseY;
            var squaredDistance = (deltaX * deltaX + deltaY * deltaY) / (uiScale * uiScale);
            if (squaredDistance > 25.0f)
            {
                // Same behavior as the game, ignore the next up if the mouse moved too much between two events.
                // See first function in case AtkEventType_MouseUp in Client::UI::AddonAreaMap_ReceiveEvent.
                _hasDragged = true;
            }

            _lastMouseX = atkEventData->MouseData.PosX;
            _lastMouseY =  atkEventData->MouseData.PosY;
        }

        if (!isClickCompleted)
        {
            return;
        }

        _isMouseDown = false;

        if (isMouseUp && isLeftClicked)
        {
            if (_hasDragged)
            {
                return;
            }
        }

        if (Bitmask.IsBitSet(FXWindows.GetKeyState((int)Keys.ControlKey), 15) ||
            Bitmask.IsBitSet(FXWindows.GetKeyState((int)Keys.LControlKey), 15) ||
            Bitmask.IsBitSet(FXWindows.GetKeyState((int)Keys.RControlKey), 15))
        {
            return;
        }

        ProcessMapClick(addonAreaMap, atkEventData);
    }

    private static void ProcessMapClick(AddonAreaMap* addonAreaMap, AtkEventData* atkEventData)
    {
        var agentMap = AgentMap.Instance();
        if  (agentMap == null || Utils.IsBusy())
        {
            return;
        }

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

                if (TryGetNearbyAetheryte(validAetherytes, addonAreaMap, out var targetAetheryte))
                {
                    ExecuteTeleport(targetAetheryte, P.ActiveAetheryte!.Value.ID, atkEventData);
                    return;
                }
            }
        }

        if (S.Data.ResidentialAethernet.ActiveAetheryte != null)
        {
            var zone = S.Data.ResidentialAethernet.ZoneInfo.SafeSelect(agentMap->SelectedTerritoryId);
            if (zone != null && TryGetNearbyAetheryte(zone.Aetherytes, addonAreaMap, out var targetAetheryte))
            {
                ExecuteTeleport(targetAetheryte, S.Data.ResidentialAethernet.ActiveAetheryte.Value.ID, atkEventData);
                return;
            }
        }

        if (S.Data.CustomAethernet.ActiveAetheryte != null)
        {
            var zone = S.Data.CustomAethernet.ZoneInfo.SafeSelect(agentMap->SelectedTerritoryId);
            if (zone != null && TryGetNearbyAetheryte(zone.Aetherytes, addonAreaMap, out var targetAetheryte))
            {
                ExecuteTeleport(targetAetheryte, S.Data.CustomAethernet.ActiveAetheryte.Value.ID, atkEventData);
                return;
            }
        }

        if (!C.DisableMapClickOtherTerritory)
        {
            var masterEntry = S.Data.DataStore.Aetherytes.FirstOrNull(x => x.Value.Any(y => y.TerritoryType == agentMap->SelectedTerritoryId));
            if (masterEntry is var (master, aetherytes))
            {
                var validAetherytes = aetherytes.Where(x => x.TerritoryType == agentMap->SelectedTerritoryId && !x.Invisible);
                if (TryGetNearbyAetheryte(validAetherytes, addonAreaMap, out var nearbyAetheryte))
                {
                    TaskAetheryteAethernetTeleport.Enqueue(master.ID, nearbyAetheryte.ID);
                }
            }
        }
    }

    private static void ExecuteTeleport(IAetheryte targetAetheryte, uint activeId, AtkEventData* atkEventData)
    {
        if (activeId == 0 || targetAetheryte == null)
        {
            return;
        }

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
    }

    private static bool TryGetNearbyAetheryte<T>(IEnumerable<T> aetherytes, AddonAreaMap* addonAreaMap, out IAetheryte nearbyAetheryte) where T : IAetheryte
    {
        nearbyAetheryte = null;

        var agentMap = AgentMap.Instance();
        if (agentMap == null || aetherytes == null || addonAreaMap == null)
        {
            return false;
        }

        var closest = aetherytes
            .Select(x =>
            {
                var mapPosition = MapUtil.WorldToMap(x.Position, -agentMap->SelectedOffsetX, -agentMap->SelectedOffsetY, (uint)agentMap->SelectedMapSizeFactor);
                var delta = Vector2.Abs(mapPosition - addonAreaMap->HoveredCoords);
                return (Aetheryte: x, ChebyshevDistance: MathF.Max(delta.X, delta.Y));
            })
            .MinBy(x => x.ChebyshevDistance);

        if (closest.Aetheryte == null)
        {
            return false;
        }

        var zoomLevel = addonAreaMap->ZoomSlider->Value; // 0 to 7 (min to max)
        var normalizedZoom = zoomLevel / 7.0f;

        const float minZoomHitThreshold = 0.6f;
        const float maxZoomHitThreshold = 0.2f;

        // the hover coordinates are truncated, on max zoom level, this is a major issue without compensation
        var hoverErrorCompensation = 0.1f * zoomLevel;

        // linear interpolation
        var baseHitThreshold = (minZoomHitThreshold * (1.0f - normalizedZoom) + maxZoomHitThreshold * normalizedZoom);
        var hitThreshold = baseHitThreshold / agentMap->SelectedMapSizeFactorFloat + hoverErrorCompensation;

        // square hit detection, the game does a similar detection (although not exactly based on this)
        if (closest.ChebyshevDistance <= hitThreshold)
        {
            nearbyAetheryte = closest.Aetheryte;
            return true;
        }

        return false;
    }
}
