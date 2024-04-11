using ECommons.GameHelpers;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;

public static class TaskReturnToGateway
{
    public static void Enqueue()
    {
        P.TaskManager.Enqueue(WaitUntilInteractable);
        P.TaskManager.Enqueue(() =>
        {
            if (Svc.ClientState.TerritoryType != P.Config.WorldChangeAetheryte.GetTerritoryType())
            {
                P.TaskManager.EnqueueImmediate(WorldChange.ExecuteTPToGatewayCommand);
                P.TaskManager.EnqueueImmediate(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], 20000, "WaitUntilBetweenAreas");
                P.TaskManager.EnqueueImmediate(WorldChange.WaitUntilNotBusy, 120000);
                P.TaskManager.EnqueueImmediate(() => Player.Interactable && Svc.ClientState.TerritoryType == P.Config.WorldChangeAetheryte.GetTerritoryType(), 120000, "WaitUntilPlayerInteractable");
            }
        }, "TaskReturnToGatewayMaster");
    }

    static bool? WaitUntilInteractable() => Player.Interactable;
}
