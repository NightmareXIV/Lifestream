using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Schedulers;
using Lifestream.Tasks;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.CrossWorld;
using Lifestream.Tasks.SameWorld;

namespace Lifestream.Systems;
public static unsafe class CharaSelectVisit
{
    public static void HomeToHome(string destinationWorld, string charaName, uint homeWorld, string secondaryTeleport = null, bool noSecondaryTeleport = false, WorldChangeAetheryte? gateway = null, bool? doNotify = null, bool? returnToGateway = null, bool noLogin = false)
    {
        ApplyDefaults(ref returnToGateway, ref gateway, ref doNotify);
        TaskReturnToHomeWorldCharaSelect.Enqueue(charaName, homeWorld);
        if(!noLogin)
        {
            TaskSelectChara.Enqueue(charaName, homeWorld);
            if(ExcelWorldHelper.Get(homeWorld)?.Name != destinationWorld)
            {
                P.TaskManager.EnqueueMulti([
                    new(WorldChange.WaitUntilNotBusy, TaskSettings.TimeoutInfinite),
                        new DelayTask(1000),
                        new(() => TaskTPAndChangeWorld.Enqueue(destinationWorld, gateway.Value.AdjustGateway(), true), $"TpAndChangeWorld {destinationWorld} at {gateway.Value}"),
                        ]);
            }
            else
            {
                TaskWaitUntilInWorld.Enqueue(destinationWorld, false);
            }
            if(gateway != null && returnToGateway == true) TaskReturnToGateway.Enqueue(gateway.Value);
            if(doNotify == true) TaskDesktopNotification.Enqueue($"Arrived to {destinationWorld}");
            EnqueueSecondary(noSecondaryTeleport, secondaryTeleport);
        }
    }

    public static void GuestToHome(string destinationWorld, string charaName, uint homeWorld, string secondaryTeleport = null, bool noSecondaryTeleport = false, WorldChangeAetheryte? gateway = null, bool? doNotify = null, bool? returnToGateway = null, bool skipReturn = false, bool noLogin = false)
    {
        ApplyDefaults(ref returnToGateway, ref gateway, ref doNotify);
        if(!skipReturn) TaskReturnToHomeDC.Enqueue(charaName, homeWorld);
        if(!noLogin)
        {
            TaskSelectChara.Enqueue(charaName, homeWorld);
            if(ExcelWorldHelper.Get(homeWorld)?.Name != destinationWorld)
            {
                P.TaskManager.EnqueueMulti([
                    new(WorldChange.WaitUntilNotBusy, TaskSettings.TimeoutInfinite),
                        new DelayTask(1000),
                        new(() => TaskTPAndChangeWorld.Enqueue(destinationWorld, gateway.Value.AdjustGateway(), true), $"TpAndChangeWorld {destinationWorld} at {gateway.Value}"),
                        ]);
            }
            else
            {
                TaskWaitUntilInWorld.Enqueue(destinationWorld, true);
            }
            if(gateway != null && returnToGateway == true) TaskReturnToGateway.Enqueue(gateway.Value);
            if(doNotify == true) TaskDesktopNotification.Enqueue($"Arrived to {destinationWorld}");
            EnqueueSecondary(noSecondaryTeleport, secondaryTeleport);
        }
    }

    public static void HomeToGuest(string destinationWorld, string charaName, uint homeWorld, string secondaryTeleport = null, bool noSecondaryTeleport = false, WorldChangeAetheryte? gateway = null, bool? doNotify = null, bool? returnToGateway = null, bool noLogin = false)
    {
        ApplyDefaults(ref returnToGateway, ref gateway, ref doNotify);
        TaskChangeDatacenter.Enqueue(destinationWorld, charaName, homeWorld);
        if(!noLogin)
        {
            TaskSelectChara.Enqueue(charaName, homeWorld);
            TaskWaitUntilInWorld.Enqueue(destinationWorld, true);
            TaskEnforceWorld.Enqueue(destinationWorld, gateway);


            if(gateway != null && returnToGateway == true) TaskReturnToGateway.Enqueue(gateway.Value);
            if(doNotify == true) TaskDesktopNotification.Enqueue($"Arrived to {destinationWorld}");
            EnqueueSecondary(noSecondaryTeleport, secondaryTeleport);
        }
    }

    public static void GuestToGuest(string destinationWorld, string charaName, uint homeWorld, string secondaryTeleport = null, bool noSecondaryTeleport = false, WorldChangeAetheryte? gateway = null, bool? doNotify = null, bool? returnToGateway = null, bool noLogin = false, bool useSameWorldReturnHome = false)
    {
        ApplyDefaults(ref returnToGateway, ref gateway, ref doNotify);
        if(useSameWorldReturnHome)
        {
            TaskReturnToHomeWorldCharaSelect.Enqueue(charaName, homeWorld);
        }
        else
        {
            TaskReturnToHomeDC.Enqueue(charaName, homeWorld);
        }
        TaskChangeDatacenter.Enqueue(destinationWorld, charaName, homeWorld);
        if(!noLogin)
        {
            TaskSelectChara.Enqueue(charaName, homeWorld);
            TaskWaitUntilInWorld.Enqueue(destinationWorld, true);
            TaskEnforceWorld.Enqueue(destinationWorld, gateway);
            if(gateway != null && returnToGateway == true) TaskReturnToGateway.Enqueue(gateway.Value);
            if(doNotify == true) TaskDesktopNotification.Enqueue($"Arrived to {destinationWorld}");
            EnqueueSecondary(noSecondaryTeleport, secondaryTeleport);
        }
    }

    public static void EnqueueSecondary(bool noSecondaryTeleport, string secondaryTeleport)
    {
        if(noSecondaryTeleport) return;
        if(secondaryTeleport == null && P.Config.WorldVisitTPToAethernet && !P.Config.WorldVisitTPTarget.IsNullOrEmpty())
        {
            secondaryTeleport = P.Config.WorldVisitTPTarget;
        }
        if(!secondaryTeleport.IsNullOrEmpty())
        {
            P.TaskManager.EnqueueMulti([
                new(() => Player.Interactable),
                    new(() => TaskTryTpToAethernetDestination.Enqueue(secondaryTeleport))
                ]);
        }
    }

    public static void ApplyDefaults(ref bool? returnToGateway, ref WorldChangeAetheryte? gateway, ref bool? doNotify)
    {
        returnToGateway ??= P.Config.DCReturnToGateway;
        gateway ??= P.Config.WorldChangeAetheryte;
        doNotify ??= true;
    }
}
