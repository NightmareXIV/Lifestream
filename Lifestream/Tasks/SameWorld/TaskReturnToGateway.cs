using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;

public static class TaskReturnToGateway
{
    public static void Enqueue(WorldChangeAetheryte gateway, bool force = false)
    {
        if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(WaitUntilInteractable);
        P.TaskManager.Enqueue(() =>
        {
            gateway = Utils.AdjustGateway(gateway);
            if(force || P.Territory != gateway.GetTerritory())
            {
                P.TaskManager.InsertMulti(
                    new(() => WorldChange.ExecuteTPToAethernetDestination((uint)gateway), $"ExecuteTPToAethernetDestination({gateway})"),
                    new(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas"),
                    new(WorldChange.WaitUntilNotBusy, TaskSettings.Timeout2M),
                    new(() => Player.Interactable && P.Territory == gateway.GetTerritory(), "WaitUntilPlayerInteractable", TaskSettings.Timeout2M)
                    );
            }
            return true;
        }, "TaskReturnToGatewayMaster");
    }

    public static bool? WaitUntilInteractable() => Player.Interactable;
}
