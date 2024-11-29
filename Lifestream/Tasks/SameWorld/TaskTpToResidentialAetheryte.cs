using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;
public static class TaskTpToResidentialAetheryte
{
    public static void Insert(ResidentialAetheryteKind target)
    {
        P.TaskManager.Insert(() => Player.Interactable && P.Territory == target.GetTerritory(), "WaitUntilPlayerInteractable", TaskSettings.Timeout2M);
        P.TaskManager.Insert(WorldChange.WaitUntilNotBusy, TaskSettings.Timeout2M);
        P.TaskManager.Insert(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
        P.TaskManager.Insert(() => WorldChange.ExecuteTPToAethernetDestination((uint)target), $"ExecuteTPToAethernetDestination {target}");
        if(P.Config.WaitForScreenReady) P.TaskManager.Insert(Utils.WaitForScreen);
    }
}
