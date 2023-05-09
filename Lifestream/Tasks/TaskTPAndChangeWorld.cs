using ECommons.GameHelpers;
using Lifestream.Schedulers;
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
                    P.TaskManager.Enqueue(WorldChange.ExecuteTPCommand);
                    P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], 20000, "WaitUntilBetweenAreas");
                    P.TaskManager.Enqueue(WorldChange.WaitUntilNotBusy, 120000);
                    P.TaskManager.Enqueue(() => Player.Interactable && Svc.ClientState.TerritoryType == Util.WCATerritories[P.Config.WorldChangeAetheryte], 120000, "WaitUntilPlayerInteractable");
                }
                P.TaskManager.Enqueue(() =>
                {
                    if((P.ActiveAetheryte == null || !P.ActiveAetheryte.Value.IsWorldChangeAetheryte()) && Util.GetReachableWorldChangeAetheryte() != null)
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
}
