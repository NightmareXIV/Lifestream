using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;

internal class TaskReturnToHomeDC
{
    internal static void Enqueue(string charaName, uint charaWorld)
    {
        PluginLog.Debug($"Beginning returning home process.");
        P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName, charaWorld), nameof(DCChange.OpenContextMenuForChara), new(timeLimitMS: 5.Minutes()));
        P.TaskManager.Enqueue(DCChange.SelectReturnToHomeWorld);
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisit, new(timeLimitMS: 2.Minutes()));
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisit2, new(timeLimitMS: 2.Minutes()));
        P.TaskManager.Enqueue(DCChange.SelectOk, new(timeLimitMS: int.MaxValue));
        P.TaskManager.Enqueue(() => DCChange.SelectServiceAccount(Utils.GetServiceAccount(charaName, charaWorld)), $"SelectServiceAccount_{charaName}@{charaWorld}", new(timeLimitMS: 1.Minutes()));
    }
}
