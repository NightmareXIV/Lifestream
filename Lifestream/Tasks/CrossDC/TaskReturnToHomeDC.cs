using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;

internal class TaskReturnToHomeDC
{
    internal static void Enqueue(string charaName, uint charaWorld)
    {
        PluginLog.Debug($"Beginning returning home process.");
        P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName, charaWorld), 5.Minutes(), nameof(DCChange.OpenContextMenuForChara));
        P.TaskManager.Enqueue(DCChange.SelectReturnToHomeWorld);
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisit, 2.Minutes());
        P.TaskManager.Enqueue(DCChange.ConfirmDcVisit2, 2.Minutes());
        P.TaskManager.Enqueue(DCChange.SelectOk, int.MaxValue);
        P.TaskManager.Enqueue(() => DCChange.SelectServiceAccount(Utils.GetServiceAccount(charaName, charaWorld)), 1.Minutes(), $"SelectServiceAccount_{charaName}@{charaWorld}");
    }
}
