using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;

internal static class TaskChangeDatacenter
{
    internal static void Enqueue(string destination, string charaName, uint charaWorld)
    {
        var dc = Utils.GetDataCenter(destination);
        PluginLog.Debug($"Beginning data center changing process. Destination: {dc}, {destination}");
        P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName, charaWorld), nameof(DCChange.OpenContextMenuForChara), TaskSettings.Timeout5M);
        P.TaskManager.Enqueue(DCChange.SelectVisitAnotherDC);
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisitIntention);
        P.TaskManager.Enqueue(() => DCChange.SelectTargetDataCenter(dc), nameof(DCChange.SelectTargetDataCenter), TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(() => DCChange.SelectTargetWorld(destination), nameof(DCChange.SelectTargetWorld), TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisit, TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisit2, TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(DCChange.SelectOk, TaskSettings.TimeoutInfinite);
        P.TaskManager.Enqueue(() => DCChange.SelectServiceAccount(Utils.GetServiceAccount(charaName, charaWorld)), $"SelectServiceAccount_{charaName}@{charaWorld}", TaskSettings.Timeout1M);
    }
}
