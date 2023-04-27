using ECommons.GameHelpers;
using ECommons.Throttlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks
{
    internal static class TaskFirmanentTeleport
    {
        internal static void Enqueue()
        {
            P.TaskManager.Enqueue(Scheduler.TargetValidAetheryte);
            P.TaskManager.Enqueue(Scheduler.InteractWithTargetedAetheryte);
             P.TaskManager.Enqueue(() =>
             {
                 if (!Player.Available) return false;
                 return Util.TrySelectSpecificEntry(Lang.TravelToFirmament, () => EzThrottler.Throttle("SelectString"));
             });
        }
    }
}
