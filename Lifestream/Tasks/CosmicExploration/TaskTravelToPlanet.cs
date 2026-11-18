using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.ExcelServices.Sheets;
using ECommons.GameFunctions;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Text;
using Callback = ECommons.Automation.Callback;
#nullable enable
#pragma warning disable CS0618

namespace Lifestream.Tasks.CosmicExploration;

public unsafe static class TaskTravelToPlanet
{
    public static void Enqueue(CosmicPlanet planet)
    {
        P.TaskManager.EnqueueTask(NeoTasks.InteractWithObject(GetNearbySwitchingNpc));
        P.TaskManager.Enqueue(() =>
        {
            if(TryGetAddonMaster<AddonMaster.SelectString>(out var m) && m.IsAddonReady)
            {
                foreach(var x in m.Entries)
                {
                    if(x.Text == Svc.Data.GetExcelSheet<QuestDialogueText>(name: "custom/009/CtsWksEntrance_00921").GetRow(18).Value.GetText())
                    {
                        x.Select();
                        return true;
                    }
                }
            }
            return false;
        });
        P.TaskManager.Enqueue(() => SelectPlanet(planet));
        P.TaskManager.Enqueue(() =>
        {
            if(TryGetAddonMaster<AddonMaster.SelectYesno>(out var m) && m.IsAddonReady)
            {
                if(m.Text == Addon.Get(17015).Text.GetText())
                {
                    m.Yes();
                    return true;
                }
            }
            return false;
        });
    }

    public static bool SelectPlanet(CosmicPlanet planet)
    {
        if(TryGetAddonByName<AtkUnitBase>("WKSPlanetSelect", out var addon) && addon->IsReady())
        {
            Callback.Fire(addon, true, 11, (uint)planet, 1u);
            return true;
        }
        return false;
    }

    public static IGameObject? GetNearbySwitchingNpc()
    {
        if(PlanetTerritory.ContainsValue(Player.Territory) && Player.Interactable && IsScreenReady())
        {
            foreach(var x in Svc.Objects)
            {
                if(x.IsTargetable && x.DataId.EqualsAny<uint>(1052624, 1056818, 1052644, 1052625) && Player.DistanceTo(x) < 6.8f && Math.Abs(Player.Position.Y - x.Position.Y) < 2f)
                {
                    return x;
                }
            }
        }
        return null;
    }

    public static bool IsHere(this CosmicPlanet planet)
    {
        return PlanetTerritory[planet] == Player.Territory;
    }

    public static Dictionary<CosmicPlanet, uint> PlanetTerritory = new()
    {
        [CosmicPlanet.Sinus_Ardorum] = 1237,
        [CosmicPlanet.Phaenna] = 1291,
        [CosmicPlanet.Oizys] = 1310,
        [CosmicPlanet.Auxesia] = 1319,
    };

    public static Dictionary<CosmicPlanet, string> Names = new()
    {
        [CosmicPlanet.Sinus_Ardorum] = PlaceName.Get(5219).Name.GetText(),
        [CosmicPlanet.Phaenna] = PlaceName.Get(5301).Name.GetText(),
        [CosmicPlanet.Oizys] = PlaceName.Get(5406).Name.GetText(),
        [CosmicPlanet.Auxesia] = PlaceName.Get(5551).Name.GetText(),
    };
}

public enum CosmicPlanet
{
    Sinus_Ardorum,
    Phaenna,
    Oizys,
    Auxesia,
}