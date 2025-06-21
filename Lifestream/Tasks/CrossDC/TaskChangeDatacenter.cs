using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;

internal static class TaskChangeDatacenter
{
    internal static int NumRetries = 0;
    internal static long RetryAt = 0;

    internal static void Enqueue(string destination, string charaName, uint charaHomeWorld, uint currentLoginWorld)
    {
        NumRetries = 0;
        EnqueueVisitTasks(destination, charaName, charaHomeWorld, currentLoginWorld);
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisit, TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(() => DCChange.ConfirmDcVisit2(destination, charaName, charaHomeWorld, currentLoginWorld), TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(DCChange.SelectOk, TaskSettings.TimeoutInfinite);
        P.TaskManager.Enqueue(() => DCChange.SelectServiceAccount(Utils.GetServiceAccount(charaName, charaHomeWorld)), $"SelectServiceAccount_{charaName}@{charaHomeWorld}", TaskSettings.Timeout1M);
    }

    private static void EnqueueVisitTasks(string destination, string charaName, uint charaHomeWorld, uint currentLoginWorld)
    {
        var dc = Utils.GetDataCenterName(destination);
        PluginLog.Debug($"Beginning data center changing process. Destination: {dc}, {destination}");
        P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName, charaHomeWorld, currentLoginWorld), nameof(DCChange.OpenContextMenuForChara), TaskSettings.Timeout5M);
        P.TaskManager.Enqueue(DCChange.SelectVisitAnotherDC);
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisitIntention);
        P.TaskManager.Enqueue(() => DCChange.SelectTargetDataCenter(dc), nameof(DCChange.SelectTargetDataCenter), TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(() => RetryAt = Environment.TickCount64 + C.DcvRetryInterval * 1000);
        P.TaskManager.Enqueue(() => DCChange.SelectTargetWorld(destination, () => RetryVisit(destination, charaName, charaHomeWorld, currentLoginWorld)), nameof(DCChange.SelectTargetWorld), TaskSettings.Timeout60M);
    }

    internal static void ProcessUnableDialogue(string destination, string charaName, uint charaWorld, uint currentLoginWorld)
    {
        if(TryGetAddonMaster<AddonMaster.SelectOk>(out var m) && m.IsAddonReady)
        {
            if(m.Text.ContainsAny(Lang.UnableToSelectWorldForDcv) && EzThrottler.Throttle("RetryVisitOnFaulire"))
            {
                m.Ok();
                P.TaskManager.Abort();
                EnqueueVisitTasks(destination, charaName, charaWorld, currentLoginWorld);
            }
        }
    }

    private static bool RetryVisit(string destination, string charaName, uint charaWorld, uint currentLoginWorld)
    {
        if(!C.EnableDvcRetry) return false;
        //PluginLog.Information($"Retrying DC visit");
        if(RetryAt == 0)
        {
            RetryAt = Environment.TickCount64 + C.DcvRetryInterval * 1000;
        }
        else if(Environment.TickCount64 > RetryAt)
        {
            RetryAt = 0;
            NumRetries++;
            if(NumRetries > C.MaxDcvRetries)
            {
                PluginLog.Warning($"DC visit retry limit exceeded");
                P.TaskManager.Abort();
                return true;
            }
            P.TaskManager.BeginStack();
            P.TaskManager.Enqueue(DCChange.CancelDcVisit);
            EnqueueVisitTasks(destination, charaName, charaWorld, currentLoginWorld);
            P.TaskManager.InsertStack();
            return true;
        }
        return false;
    }
}
