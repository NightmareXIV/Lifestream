using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;

internal static class TaskChangeDatacenter
{
    internal static void Enqueue(string destination, string charaName, uint charaWorld)
    {
        var dc = Utils.GetDataCenter(destination);
        PluginLog.Debug($"Beginning data center changing process. Destination: {dc}, {destination}");
        P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName, charaWorld), nameof(DCChange.OpenContextMenuForChara), new(timeLimitMS: 5.Minutes()));
        P.TaskManager.Enqueue(DCChange.SelectVisitAnotherDC);
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisitIntention);
        P.TaskManager.Enqueue(() => DCChange.SelectTargetDataCenter(dc), nameof(DCChange.SelectTargetDataCenter), new(timeLimitMS: 2.Minutes()));
        P.TaskManager.Enqueue(() => DCChange.SelectTargetWorld(destination), nameof(DCChange.SelectTargetWorld), new(timeLimitMS: 2.Minutes()));
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisit, new(timeLimitMS: 2.Minutes()));
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisit2, new(timeLimitMS: 2.Minutes()));
        P.TaskManager.Enqueue(DCChange.SelectOk, new(timeLimitMS: int.MaxValue));
        P.TaskManager.Enqueue(() => DCChange.SelectServiceAccount(Utils.GetServiceAccount(charaName, charaWorld)), $"SelectServiceAccount_{charaName}@{charaWorld}", new(timeLimitMS: 1.Minutes()));
    }
}
