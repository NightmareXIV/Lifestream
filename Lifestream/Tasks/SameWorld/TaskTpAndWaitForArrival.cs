using ECommons.GameHelpers;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;
public static class TaskTpAndWaitForArrival
{
    public static void Enqueue(uint aetheryte)
    {
        P.TaskManager.EnqueueMulti(
            new(() => Player.Interactable && IsScreenReady(), "WaitUntilPlayerInteractable"),
            new(() => WorldChange.ExecuteTPToAethernetDestination(aetheryte), $"ExecuteTPToAethernetDestination({aetheryte})"),
            new(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas"),
            new(() => Player.Interactable, "WaitUntilPlayerInteractable", TaskSettings.Timeout2M)
        );
    }
}
