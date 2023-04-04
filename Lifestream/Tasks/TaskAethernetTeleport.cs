using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks
{
    internal static class TaskAethernetTeleport
    {
        internal static void Enqueue(TinyAetheryte a)
        {
            P.TaskManager.Enqueue(Scheduler.TargetValidAetheryte);
            P.TaskManager.Enqueue(Scheduler.InteractWithTargetedAetheryte);
            P.TaskManager.Enqueue(Scheduler.SelectAethernet);
            P.TaskManager.Enqueue(() => Scheduler.TeleportToAethernetDestination(a));
        }
    }
}
