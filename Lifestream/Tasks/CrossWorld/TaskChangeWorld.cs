﻿using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossWorld;

internal static unsafe class TaskChangeWorld
{
    internal static void Enqueue(string world)
    {
        if(P.Config.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        if(P.Config.LeavePartyBeforeWorldChange)
        {
            P.TaskManager.Enqueue(WorldChange.LeaveParty);
        }
        P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
        P.TaskManager.Enqueue(WorldChange.SelectVisitAnotherWorld);
        P.TaskManager.Enqueue(() => WorldChange.SelectWorldToVisit(world), $"{nameof(WorldChange.SelectWorldToVisit)}, {world}");
        P.TaskManager.Enqueue(() => WorldChange.ConfirmWorldVisit(world), $"{nameof(WorldChange.ConfirmWorldVisit)}, {world}");
        TaskWaitUntilInWorld.Enqueue(world);
    }
}
