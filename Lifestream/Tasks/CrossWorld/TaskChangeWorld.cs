using ECommons.GameHelpers;
using ECommons.Throttlers;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossWorld;

internal static unsafe class TaskChangeWorld
{
    internal static void Enqueue(string world)
    {
        if(P.Config.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        if(P.Config.LeavePartyBeforeWorldChange)
        {
            P.TaskManager.Enqueue(WorldChange.LeaveParty);
        }
        P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
        P.TaskManager.Enqueue(WorldChange.SelectVisitAnotherWorld);
        P.TaskManager.Enqueue(() => WorldChange.SelectWorldToVisit(world), $"{nameof(WorldChange.SelectWorldToVisit)}, {world}");
        P.TaskManager.Enqueue(() => WorldChange.ConfirmWorldVisit(world), $"{nameof(WorldChange.ConfirmWorldVisit)}, {world}");
        P.TaskManager.Enqueue((Action)(() => EzThrottler.Throttle("RetryWorldVisit", Math.Max(10000, P.Config.RetryWorldVisitInterval * 1000), true)));
        P.TaskManager.Enqueue(() => RetryWorldVisit(world), TaskSettings.Timeout5M);
    }

    private static bool RetryWorldVisit(string targetWorld)
    {
        if(Player.Available && Player.CurrentWorld == targetWorld)
        {
            return true;
        }
        if(P.Config.RetryWorldVisit)
        {
            if(!IsScreenReady() || Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld] || Svc.Condition[ConditionFlag.ReadyingVisitOtherWorld] || Svc.Condition[ConditionFlag.OccupiedInQuestEvent])
            {
                EzThrottler.Throttle("RetryWorldVisit", Math.Max(1000, P.Config.RetryWorldVisitInterval * 1000), true);
                return false;
            }
            if(EzThrottler.Check("RetryWorldVisit"))
            {
                P.TaskManager.BeginStack();
                TaskChangeWorld.Enqueue(targetWorld);
                P.TaskManager.EnqueueStack();
                return true;
            }
        }
        return false;
    }
}
