using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld
{
    internal static class TaskAethernetTeleport
    {
        internal static void Enqueue(TinyAetheryte a)
        {
            P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
            P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
            if (P.DataStore.Aetherytes.ContainsKey(P.ActiveAetheryte.Value)) P.TaskManager.Enqueue(WorldChange.SelectAethernet);
            P.TaskManager.DelayNext(P.Config.SlowTeleport ? P.Config.SlowTeleportThrottle : 0);
            P.TaskManager.Enqueue(() => WorldChange.TeleportToAethernetDestination(a), nameof(WorldChange.TeleportToAethernetDestination));
        }
    }
}
