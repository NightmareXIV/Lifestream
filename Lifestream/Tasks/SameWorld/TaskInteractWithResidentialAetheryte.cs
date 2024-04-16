using ECommons.Automation.NeoTaskManager;
using ECommons.Automation.NeoTaskManager.Tasks;
using Lifestream.Schedulers;
using Lifestream.Tasks.CrossWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.SameWorld;
public static class TaskInteractWithResidentialAetheryte
{
    public static void Insert()
    {
        if (P.Config.WaitForScreen) P.TaskManager.EnqueueTask(new(Utils.WaitForScreen));
        if (P.ActiveAetheryte != null && P.ActiveAetheryte.Value.IsResidentialAetheryte())
        {
            //TaskChangeWorld.Enqueue(world);
        }
        else
        {
            P.TaskManager.EnqueueTask(new(() =>
            {
                if ((P.ActiveAetheryte == null || !P.ActiveAetheryte.Value.IsWorldChangeAetheryte()) && Utils.GetReachableWorldChangeAetheryte() != null)
                {
                    P.TaskManager.InsertMulti([
                        new FrameDelayTask(10),
                        new(WorldChange.TargetReachableAetheryte),
                        new(WorldChange.LockOn),
                        new(WorldChange.EnableAutomove),
                        new(WorldChange.WaitUntilWorldChangeAetheryteExists),
                        new(WorldChange.DisableAutomove),
                        ]);
                }
            }, "ConditionalLockonTask"));
            P.TaskManager.EnqueueTask(new(WorldChange.WaitUntilWorldChangeAetheryteExists));
            P.TaskManager.EnqueueDelay(10, true);
            //TaskChangeWorld.Enqueue(world);
        }
    }
}
