using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Schedulers;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Utility;
using System.Linq;

namespace Lifestream.Tasks.CrossDC;
public static class TaskTpAndGoToWard
{
    public static void Enqueue(string world, ResidentialAetheryte residentialArtheryte, int ward, int plot)
    {
        var gateway = DetermineGatewayAetheryte(residentialArtheryte);
        if (Player.CurrentWorld != world)
        {
            if (P.DataStore.Worlds.TryGetFirst(x => x.StartsWith(world == "" ? Player.HomeWorld : world, StringComparison.OrdinalIgnoreCase), out var w))
            {
                P.TPAndChangeWorld(w, false, null, true, gateway, false, gateway != null);
            }
            else if (P.DataStore.DCWorlds.TryGetFirst(x => x.StartsWith(world == "" ? Player.HomeWorld : world, StringComparison.OrdinalIgnoreCase), out var dcw))
            {
                P.TPAndChangeWorld(dcw, true, null, true, gateway, false, gateway != null);
            }
        }
        P.TaskManager.Enqueue(TaskReturnToGateway.WaitUntilInteractable);
        if (P.Config.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(() =>
        {
            if (Svc.ClientState.TerritoryType != residentialArtheryte.GetTerritory())
            {
                TaskTpToResidentialAetheryte.Insert(residentialArtheryte);
            }
        }, "TaskTpToResidentialAetheryteIfNeeded");
        P.TaskManager.Enqueue(() => Utils.GetReachableAetheryte(x => x.ObjectKind == ObjectKind.Aetheryte) != null, "WaitUntilReachableAetheryteExists");
        TaskApproachAetheryteIfNeeded.Enqueue();
        TaskGoToResidentialDistrict.Enqueue(ward);
        if (P.ResidentialAethernet.HousingData.Data.TryGetValue(residentialArtheryte.GetResidentialTerritory(), out var plotInfos))
        {
            var info = plotInfos.SafeSelect(plot);
            if (info != null)
            {
                if (!P.ResidentialAethernet.StartingAetherytes.Contains(info.AethernetID))
                {
                    TaskApproachHousingAetheryte.Enqueue();
                    var aetheryte = P.ResidentialAethernet.ZoneInfo.SafeSelect(residentialArtheryte.GetResidentialTerritory())?.Aetherytes.FirstOrDefault(x => x.ID == info.AethernetID);
                    if (aetheryte != null)
                    {
                        TaskAethernetTeleport.Enqueue(aetheryte.Value.Name);
                        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
                        P.TaskManager.Enqueue(Utils.WaitForScreen);
                        //P.TaskManager.Enqueue(P.VnavmeshManager.IsReady);
                        //P.TaskManager.Enqueue(() => P.VnavmeshManager.PathfindAndMoveTo(info.Front, false));
                    }
                }
                TaskMoveToHouse.Enqueue(info);
            }
        }
    }

    public static WorldChangeAetheryte? DetermineGatewayAetheryte(ResidentialAetheryte targetZone)
    {
        if (targetZone == ResidentialAetheryte.Uldah) return WorldChangeAetheryte.Uldah;
        if (targetZone == ResidentialAetheryte.Gridania) return WorldChangeAetheryte.Gridania;
        if (targetZone == ResidentialAetheryte.Limsa) return WorldChangeAetheryte.Limsa;
        return null;
    }
}
