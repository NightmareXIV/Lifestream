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
        if(mount ?? C.UseMount)
        {
            if(!TaskMount.MountIfCan())
            {
                return false;
            }
        }
        if(!C.UseSprintPeloton && !C.UsePeloton) return true;
        if(Player.Object.StatusList.Any(x => x.StatusId.EqualsAny<uint>(50, 1199, 4209))) return true;
        List<uint> abilities = [];
        if(C.UseSprintPeloton) abilities.Add(3);
        if(C.UsePeloton) abilities.Add(7557);
        foreach(var ability in abilities)
        {
            if(ActionManager.Instance()->GetActionStatus(ActionType.Action, ability) == 0)
            {
                if(EzThrottler.Throttle("ExecSpritAction"))
                {
                    Chat.ExecuteCommand($"/action \"{Svc.Data.GetExcelSheet<Action>().GetRow(ability).Name.GetText()}\"");
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
