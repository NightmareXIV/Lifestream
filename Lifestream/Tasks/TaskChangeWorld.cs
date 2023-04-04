using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks
{
    internal static unsafe class TaskChangeWorld
    {
        internal static void Enqueue(string world)
        {
            P.TaskManager.Enqueue(Scheduler.TargetValidAetheryte);
            P.TaskManager.Enqueue(Scheduler.InteractWithTargetedAetheryte);
            P.TaskManager.Enqueue(Scheduler.SelectVisitAnotherWorld);
            P.TaskManager.Enqueue(() => Scheduler.SelectWorldToVisit(world));
            P.TaskManager.Enqueue(() => Scheduler.ConfirmWorldVisit(world));
        }
    }
}
