using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;
public static class TaskTpToResidentialAetheryte
{
    public static void Insert(ResidentialAetheryte target)
    {
        P.TaskManager.Insert(() => Player.Interactable && Svc.ClientState.TerritoryType == target.GetTerritory(), "WaitUntilPlayerInteractable", new(timeLimitMS: 120000));
        P.TaskManager.Insert(WorldChange.WaitUntilNotBusy, new(timeLimitMS: 120000));
        P.TaskManager.Insert(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
        P.TaskManager.Insert(() => WorldChange.ExecuteTPToAethernetDestination((uint)target), $"ExecuteTPToAethernetDestination {target}");
        if (P.Config.WaitForScreenReady) P.TaskManager.Insert(Utils.WaitForScreen);
    }
}
