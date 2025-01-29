using ECommons.Automation;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lifestream.Data;
using Action = Lumina.Excel.Sheets.Action;

namespace Lifestream.Tasks.Utility;
public static unsafe class TaskMoveToHouse
{
    public static void Enqueue(PlotInfo info, bool includeFirst)
    {
        P.TaskManager.EnqueueMulti(
            new(() => UseSprint()),
            new(() => LoadPath(info, includeFirst), "LoadPath"),
            new(WaitUntilPathCompleted, TaskSettings.Timeout5M)
            );
    }

    public static bool? UseSprint(bool? mount = null)
    {
        if(Player.IsAnimationLocked) return false;
        if(mount ?? P.Config.UseMount)
        {
            if(Svc.Condition[ConditionFlag.Casting] || Svc.Condition[ConditionFlag.Unknown57])
            {
                EzThrottler.Throttle("MountForceStop", 200, true);
                return false;
            }
            if(Svc.Condition[ConditionFlag.Mounted]) return true;
            if(!EzThrottler.Check("MountForceStop")) return false;
            if(ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, 9) == 0)
            {
                if(EzThrottler.Throttle("UseMount", 3000))
                {
                    Chat.Instance.ExecuteGeneralAction(9);
                }
            }
            else
            {
                return true;
            }
            return false;
        }
        if(!P.Config.UseSprintPeloton) return true;
        if(Player.Object.StatusList.Any(x => x.StatusId.EqualsAny<uint>(50, 1199))) return true;
        uint[] abilities = [3, 7557];
        foreach(var ability in abilities)
        {
            if(ActionManager.Instance()->GetActionStatus(ActionType.Action, ability) == 0)
            {
                if(EzThrottler.Throttle("ExecSpritAction"))
                {
                    Chat.Instance.ExecuteCommand($"/action \"{Svc.Data.GetExcelSheet<Action>().GetRow(ability).Name.GetText()}\"");
                    return true;
                }
            }
        }
        return true;
    }

    public static bool? LoadPath(PlotInfo info, bool includeFirst)
    {
        if(info.Path.Count == 0) return null;
        P.FollowPath.Stop();
        P.FollowPath.Move([.. info.Path], true);
        if(!includeFirst) P.FollowPath.RemoveFirst();
        return true;
    }

    public static bool WaitUntilPathCompleted() => P.FollowPath.Waypoints.Count == 0;
}
