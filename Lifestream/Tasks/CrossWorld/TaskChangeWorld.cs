using ECommons.GameHelpers;
using ECommons.Throttlers;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossWorld;

internal static unsafe class TaskChangeWorld
{
    internal static void Enqueue(string world)
    {
        try
        {
            Utils.AssertCanTravel(Player.Name, Player.Object.HomeWorld.RowId, Player.Object.CurrentWorld.RowId, world);
        }
        catch(Exception e) { e.Log(); return; }
        if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        if(C.LeavePartyBeforeWorldChange)
        {
            if(Svc.Condition[ConditionFlag.RecruitingWorldOnly])
            {
                P.TaskManager.Enqueue(WorldChange.ClosePF);
                P.TaskManager.Enqueue(WorldChange.OpenSelfPF);
                P.TaskManager.Enqueue(WorldChange.EndPF);
                P.TaskManager.Enqueue(WorldChange.WaitUntilNotRecruiting);
            }
            P.TaskManager.Enqueue(WorldChange.LeaveParty);
        }
        P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
        P.TaskManager.Enqueue(WorldChange.SelectVisitAnotherWorld);
        P.TaskManager.Enqueue(() => WorldChange.SelectWorldToVisit(world), $"{nameof(WorldChange.SelectWorldToVisit)}, {world}");
        P.TaskManager.Enqueue(() => WorldChange.ConfirmWorldVisit(world), $"{nameof(WorldChange.ConfirmWorldVisit)}, {world}");
        P.TaskManager.Enqueue((Action)(() => EzThrottler.Throttle("RetryWorldVisit", Math.Max(10000, C.RetryWorldVisitInterval * 1000), true)));
        P.TaskManager.Enqueue(() => RetryWorldVisit(world), TaskSettings.Timeout5M);
    }

    private static int WorldVisitRand = 0;
    private static bool RetryWorldVisit(string targetWorld)
    {
        if(Player.Available && Player.CurrentWorld == targetWorld)
        {
            return true;
        }
        if(C.RetryWorldVisit)
        {
            if(!IsScreenReady() || Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld] || Svc.Condition[ConditionFlag.ReadyingVisitOtherWorld] || Svc.Condition[ConditionFlag.OccupiedInQuestEvent])
            {
                EzThrottler.Throttle("RetryWorldVisit", Math.Max(1000, C.RetryWorldVisitInterval * 1000 + WorldVisitRand), true);
                return false;
            }
            if(Svc.Targets.Target == null && Player.Interactable)
            {
                var aetheryte = Svc.Objects.FirstOrDefault(x => x.IsTargetable && x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Aetheryte && Player.DistanceTo(x) < 30f);
                if(aetheryte != null && EzThrottler.Throttle("Target"))
                {
                    Svc.Targets.Target = aetheryte;
                }
            }
            if(EzThrottler.Check("RetryWorldVisit"))
            {
                WorldVisitRand = Random.Shared.Next(0, C.RetryWorldVisitIntervalDelta * 1000);
                P.TaskManager.BeginStack();
                TaskChangeWorld.Enqueue(targetWorld);
                P.TaskManager.InsertStack();
                return true;
            }
        }
        return false;
    }
}
