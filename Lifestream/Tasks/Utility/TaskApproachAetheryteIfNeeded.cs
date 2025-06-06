using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.Utility;
public static class TaskApproachAetheryteIfNeeded
{
    public static void Enqueue()
    {
        P.TaskManager.Enqueue(() =>
        {
            if(Utils.ApproachConditionIsMet())
            {
                P.TaskManager.InsertMulti(
                    C.WaitForScreenReady ? new(Utils.WaitForScreen) : null,
                    new FrameDelayTask(10),
                    new(TargetReachableAetheryte),
                    new(WorldChange.LockOn),
                    new(WorldChange.EnableAutomove),
                    new(WaitUntilAetheryteExists),
                    new(WorldChange.DisableAutomove)
                );
            }
        }, "TaskApproachAetheryteIfNeeded Master Task");
    }

    public static bool WaitUntilAetheryteExists()
    {
        if(!Player.Available) return false;
        return P.ActiveAetheryte != null && P.ActiveAetheryte.Value.IsAetheryte;
    }


    public static bool TargetReachableAetheryte()
    {
        if(!Player.Available) return false;
        var a = Utils.GetReachableAetheryte(x => x.ObjectKind == ObjectKind.Aetheryte);
        if(a != null)
        {
            if(!a.IsTarget() && EzThrottler.Throttle("TargetReachableAetheryte", 200))
            {
                Svc.Targets.SetTarget(a);
                return true;
            }
        }
        return false;
    }
}
