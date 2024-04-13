using ECommons.GameHelpers;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskTpToGateway
{
    internal static void Enqueue()
    {
        if (P.Config.WaitForScreen) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(WorldChange.ExecuteTPToGatewayCommand);
        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], 20000, "WaitUntilBetweenAreas");
        P.TaskManager.Enqueue(WorldChange.WaitUntilNotBusy, 120000);
        P.TaskManager.Enqueue(() => Player.Interactable && Svc.ClientState.TerritoryType == Utils.WCATerritories[P.Config.WorldChangeAetheryte], 120000, "WaitUntilPlayerInteractable");
    }
}
