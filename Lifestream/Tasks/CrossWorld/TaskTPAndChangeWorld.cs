using Lifestream.Schedulers;
using Lifestream.Tasks.SameWorld;

namespace Lifestream.Tasks.CrossWorld;

internal static class TaskTPAndChangeWorld
{
    internal static void Enqueue(string world)
    {
        if (P.ActiveAetheryte != null && P.ActiveAetheryte.Value.IsWorldChangeAetheryte())
        {
            TaskChangeWorld.Enqueue(world);
        }
        else
        {
            if (Util.GetReachableWorldChangeAetheryte(!P.Config.WalkToAetheryte) == null)
            {
                TaskTpToGateway.Enqueue();
            }
            P.TaskManager.Enqueue(() =>
            {
                if ((P.ActiveAetheryte == null || !P.ActiveAetheryte.Value.IsWorldChangeAetheryte()) && Util.GetReachableWorldChangeAetheryte() != null)
                {
                    P.TaskManager.DelayNextImmediate(10, true);
                    P.TaskManager.EnqueueImmediate(WorldChange.TargetReachableAetheryte);
                    P.TaskManager.EnqueueImmediate(WorldChange.LockOn);
                    P.TaskManager.EnqueueImmediate(WorldChange.EnableAutomove);
                    P.TaskManager.EnqueueImmediate(WorldChange.WaitUntilWorldChangeAetheryteExists);
                    P.TaskManager.EnqueueImmediate(WorldChange.DisableAutomove);
                }
            }, "ConditionalLockonTask");
            P.TaskManager.Enqueue(WorldChange.WaitUntilWorldChangeAetheryteExists);
            P.TaskManager.DelayNext(10, true);
            TaskChangeWorld.Enqueue(world);
        }
    }
}
