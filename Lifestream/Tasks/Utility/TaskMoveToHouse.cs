using Lifestream.Data;
using Lifestream.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.Utility;
public static class TaskMoveToHouse
{
    public static void Enqueue(PlotInfo info, bool includeFirst)
    {
				P.TaskManager.EnqueueMulti(
            new(() => LoadPath(info, includeFirst), "LoadPath"),
            new(WaitUntilPathCompleted, TaskSettings.Timeout5M)
            );
    }

    public static bool? LoadPath(PlotInfo info, bool includeFirst)
    {
        if (info.Path.Count == 0) return null;
        P.FollowPath.Stop();
        P.FollowPath.Move([.. info.Path], true);
        if (!includeFirst) P.FollowPath.RemoveFirst();
        return true;
    }

    public static bool WaitUntilPathCompleted() => P.FollowPath.Waypoints.Count == 0;
}
