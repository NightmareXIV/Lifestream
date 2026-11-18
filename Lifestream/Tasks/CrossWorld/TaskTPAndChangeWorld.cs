using ECommons.Automation.NeoTaskManager.Tasks;
using Lifestream.Enums;
using Lifestream.Schedulers;
using Lifestream.Tasks.SameWorld;

namespace Lifestream.Tasks.CrossWorld;

internal static class TaskTPAndChangeWorld
{
    internal static void Enqueue(string world, WorldChangeAetheryte gateway, bool insert)
    {
        void action()
        {
            if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
            P.UpdateAetherytes();
            if(P.ActiveAetheryte != null && P.ActiveAetheryte.Value.IsWorldChangeAetheryte())
            {
                TaskChangeWorld.Enqueue(world);
            }
            else
            {
                if(Utils.GetReachableWorldChangeAetheryte(!C.WalkToAetheryte) == null)
                {
                    TaskTpToAethernetDestination.Enqueue(gateway);
                }
                P.TaskManager.EnqueueTask(new(() =>
                {
                    P.UpdateAetherytes();
                    if((P.ActiveAetheryte == null || !P.ActiveAetheryte.Value.IsWorldChangeAetheryte()) && Utils.GetReachableWorldChangeAetheryte() != null)
                    {
                        P.TaskManager.InsertMulti(
                            new FrameDelayTask(10),
                            new(WorldChange.TargetReachableWorldChangeAetheryte),
                            new(WorldChange.LockOn),
                            new(WorldChange.EnableAutomove),
                            new(WorldChange.WaitUntilMasterAetheryteExists),
                            new(WorldChange.DisableAutomove)
                            );
                    }
                }, "ConditionalLockonTask"));
                P.TaskManager.Enqueue(WorldChange.WaitUntilMasterAetheryteExists);
                P.TaskManager.EnqueueDelay(10, true);
                TaskChangeWorld.Enqueue(world);
            }
        }
        if(insert)
        {
            P.TaskManager.InsertStack($"{nameof(TaskTPAndChangeWorld)}: {nameof(Enqueue)}", action);
        }
        else
        {
            P.TaskManager.EnqueueStack($"{nameof(TaskTPAndChangeWorld)}: {nameof(Enqueue)}", action);
        }
    }
}
