using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskTpToAethernetDestination
{
    internal static void Enqueue(WorldChangeAetheryte worldChangeAetheryte)
    {
        if(P.Config.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(() => WorldChange.ExecuteTPToAethernetDestination((uint)worldChangeAetheryte));
        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
        P.TaskManager.Enqueue(WorldChange.WaitUntilNotBusy, TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(() => Player.Interactable && P.Territory == worldChangeAetheryte.GetTerritory(), "WaitUntilPlayerInteractable", TaskSettings.Timeout2M);
    }
}
