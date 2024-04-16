using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Systems.Legacy;
using Lifestream.Tasks.SameWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.CrossDC;
public static class TaskTpAndGoToWard
{
    public static void Enqueue(string primary, ResidentialAetheryte zone, int ward)
    {
        var gateway = DetermineGatewayAetheryte(zone);
        if (Player.CurrentWorld != primary)
        {
            if (P.DataStore.Worlds.TryGetFirst(x => x.StartsWith(primary == "" ? Player.HomeWorld : primary, StringComparison.OrdinalIgnoreCase), out var w))
            {
                P.TPAndChangeWorld(w, false, null, true, gateway, false, gateway != null);
            }
            else if (P.DataStore.DCWorlds.TryGetFirst(x => x.StartsWith(primary == "" ? Player.HomeWorld : primary, StringComparison.OrdinalIgnoreCase), out var dcw))
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
        });
    }

    public static WorldChangeAetheryte? DetermineGatewayAetheryte(ResidentialAetheryte targetZone)
    {
        if (targetZone == ResidentialAetheryte.Uldah) return WorldChangeAetheryte.Uldah;
        if (targetZone == ResidentialAetheryte.Gridania) return WorldChangeAetheryte.Gridania;
        if (targetZone == ResidentialAetheryte.Limsa) return WorldChangeAetheryte.Limsa;
        return null;
    }
}
