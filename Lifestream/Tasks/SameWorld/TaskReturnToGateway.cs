using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;

public static class TaskReturnToGateway
{
    public static void Enqueue(WorldChangeAetheryte gateway)
    {
        if (P.Config.WaitForScreen) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(WaitUntilInteractable);
        P.TaskManager.Enqueue(() =>
        {
            if (Svc.ClientState.TerritoryType != gateway.GetTerritory())
            {
                P.TaskManager.InsertMulti([
                    new(() => WorldChange.ExecuteTPToAethernetDestination((uint)gateway), $"ExecuteTPToAethernetDestination({gateway})"),
                    new(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas"),
                    new(WorldChange.WaitUntilNotBusy, new(timeLimitMS:120000)),
                    new(() => Player.Interactable && Svc.ClientState.TerritoryType == gateway.GetTerritory(), "WaitUntilPlayerInteractable", new(timeLimitMS:120000))
                    ]);
            }
        }, "TaskReturnToGatewayMaster");
    }

    public static bool? WaitUntilInteractable() => Player.Interactable;
}
