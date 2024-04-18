using Lifestream.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.Utility;
public static class TaskMoveToHouse
{
    public static void Enqueue(PlotInfo info)
    {
        P.TaskManager.EnqueueMulti(
            new(() => LoadPath(info), "LoadPath"),
            new(WaitUntilPathCompleted, TaskSettings.Timeout5M)
            );
    }

    public static bool? LoadPath(PlotInfo info)
    {
        if (info.Path.Count == 0) return null;
        P.FollowPath.Stop();
        P.FollowPath.Waypoints = [.. info.Path];
        if (!P.ResidentialAethernet.StartingAetherytes.Contains(info.AethernetID)) P.FollowPath.Waypoints.RemoveAt(0);
        return true;
    }

    public static bool WaitUntilPathCompleted() => P.FollowPath.Waypoints.Count == 0;
}
