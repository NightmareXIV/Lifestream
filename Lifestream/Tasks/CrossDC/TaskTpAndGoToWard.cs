using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Utility;

namespace Lifestream.Tasks.CrossDC;
public static class TaskTpAndGoToWard
{
    public static void Enqueue(string world, ResidentialAetheryte zone, int ward)
    {
        var gateway = DetermineGatewayAetheryte(zone);
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
        if (P.Config.WaitForScreen) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(() =>
        {
            if (Svc.ClientState.TerritoryType != zone.GetTerritory())
            {
                TaskTpToResidentialAetheryte.Insert(zone);
            }
        }, "TaskTpToResidentialAetheryteIfNeeded");
        P.TaskManager.Enqueue(() => Utils.GetReachableAetheryte(x => x.ObjectKind == ObjectKind.Aetheryte) != null, "WaitUntilReachableAetheryteExists");
        TaskApproachAetheryteIfNeeded.Enqueue();
        TaskGoToResidentialDistrict.Enqueue(ward);
    }

    public static WorldChangeAetheryte? DetermineGatewayAetheryte(ResidentialAetheryte targetZone)
    {
        if (targetZone == ResidentialAetheryte.Uldah) return WorldChangeAetheryte.Uldah;
        if (targetZone == ResidentialAetheryte.Gridania) return WorldChangeAetheryte.Gridania;
        if (targetZone == ResidentialAetheryte.Limsa) return WorldChangeAetheryte.Limsa;
        return null;
    }
}
