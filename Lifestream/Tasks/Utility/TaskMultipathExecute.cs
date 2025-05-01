using ECommons.Automation;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lifestream.Data;

namespace Lifestream.Tasks.Utility;
public static unsafe class TaskMultipathExecute
{
    private static uint LastTerritory = 0;

    public static void Enqueue(MultiPath path)
    {
        LastTerritory = 0;
        P.TaskManager.Enqueue(() => Execute(path), $"ExecuteMultipath {path.Name}", TaskSettings.TimeoutInfinite);
    }

    private static bool Execute(MultiPath mpath)
    {
        if(!Player.Interactable || !IsScreenReady())
        {
            P.FollowPath.Stop();
            return false;
        }
        var path = mpath.Entries.FirstOrDefault(x => x.Territory == P.Territory);
        if(P.Territory != LastTerritory)
        {
            P.FollowPath.Stop();
            var points = (List<Vector3>)[.. path.Points];
            var distance = float.MaxValue;
            var index = 0;
            for(var i = 0; i < points.Count; i++)
            {
                if(Vector3.Distance(Player.Object.Position, points[i]) < distance)
                {
                    index = i;
                    distance = Vector3.Distance(Player.Object.Position, points[i]);
                }
            }
            points = points[index..];
            P.FollowPath.Move(points, true);
            LastTerritory = path.Territory;
        }
        else
        {
            if(Svc.Condition[ConditionFlag.InCombat] || path?.Sprint == true)
            {
                var status = ActionManager.Instance()->GetActionStatus(ActionType.Action, 3);
                if(status == 0)
                {
                    if(EzThrottler.Throttle("UseSprint", 250))
                    {
                        Chat.ExecuteCommand("/action Sprint");
                    }
                }
            }
            P.FollowPath.UpdateTimeout(10);
            if(P.FollowPath.Waypoints.Count == 0)
            {
                P.NotificationMasterApi.DisplayTrayNotification("Multipath completed");
                return true;
            }
        }
        return false;
    }
}
