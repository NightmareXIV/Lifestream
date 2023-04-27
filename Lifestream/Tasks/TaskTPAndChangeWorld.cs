using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks
{
    internal static class TaskTPAndChangeWorld
    {
        internal static void Enqueue(string world)
        {
            if(P.ActiveAetheryte != null && P.ActiveAetheryte.Value.IsWorldChangeAetheryte())
            {
                TaskChangeWorld.Enqueue(world);
            }
            else
            {
                P.TaskManager.Enqueue(Scheduler.ExecuteTPCommand);
                P.TaskManager.Enqueue(Scheduler.WaitUntilNextToAetheryteAndNotBusy, 30000);
                P.TaskManager.DelayNext(10, true);
                TaskChangeWorld.Enqueue(world);
            }
        }
    }
}
