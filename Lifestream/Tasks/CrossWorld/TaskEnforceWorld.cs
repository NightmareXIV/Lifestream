using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.CrossWorld;
public static class TaskEnforceWorld
{
    public static void Enqueue(string destinationWorld, WorldChangeAetheryte? gateway)
    {
        P.TaskManager.Enqueue(() =>
        {
            if(Player.Interactable && IsScreenReady())
            {
                if(Player.CurrentWorld != destinationWorld)
                {
                    P.TaskManager.InsertMulti([
                        new(WorldChange.WaitUntilNotBusy, TaskSettings.TimeoutInfinite),
                        new DelayTask(1000),
                        new(() => TaskTPAndChangeWorld.Enqueue(destinationWorld, gateway.Value.AdjustGateway(), true), $"TpAndChangeWorld {destinationWorld} at {gateway.Value}"),
                        new(() => TaskWaitUntilInWorld.Task(destinationWorld, false))
                            ]);
                }
                return true;
            }
            return false;
        });
    }
}
