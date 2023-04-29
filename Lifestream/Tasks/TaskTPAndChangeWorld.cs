using ECommons.GameHelpers;
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
                if (Util.GetReachableWorldChangeAetheryte(!P.Config.WalkToAetheryte) == null)
                {
                    P.TaskManager.Enqueue(Scheduler.ExecuteTPCommand);
                    P.TaskManager.Enqueue(Scheduler.WaitUntilNotBusy, 120000);
                    P.TaskManager.Enqueue(() => Player.Interactable && Svc.ClientState.TerritoryType == Util.WCATerritories[P.Config.WorldChangeAetheryte], 120000, "WaitUntilPlayerInteractable");
                }
                P.TaskManager.Enqueue(() =>
                {
                    if((P.ActiveAetheryte == null || !P.ActiveAetheryte.Value.IsWorldChangeAetheryte()) && Util.GetReachableWorldChangeAetheryte() != null)
                    {
                        P.TaskManager.DelayNextImmediate(10, true);
                        P.TaskManager.EnqueueImmediate(Scheduler.TargetReachableAetheryte);
                        P.TaskManager.EnqueueImmediate(Scheduler.LockOn);
                        P.TaskManager.EnqueueImmediate(Scheduler.EnableAutomove);
                        P.TaskManager.EnqueueImmediate(Scheduler.WaitUntilWorldChangeAetheryteExists);
                        P.TaskManager.EnqueueImmediate(Scheduler.DisableAutomove);
                    }
                }, "ConditionalLockonTask");
                P.TaskManager.Enqueue(Scheduler.WaitUntilWorldChangeAetheryteExists);
                P.TaskManager.DelayNext(10, true);
                TaskChangeWorld.Enqueue(world);
            }
        }
    }
}
