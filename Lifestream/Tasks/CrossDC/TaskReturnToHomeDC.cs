using Lifestream.Schedulers;
using Lifestream.Tasks.Login;

namespace Lifestream.Tasks.CrossDC;

internal class TaskReturnToHomeDC
{
    internal static void Enqueue(string charaName, uint charaWorld, uint currentLoginWorld)
    {
        void tasks()
        {
            PluginLog.Debug($"Beginning returning home process.");
            P.TaskManager.Enqueue(TaskChangeCharacter.ResetWorldIndex);
        P.TaskManager.Enqueue(TaskChangeCharacter.ResetWorldIndex);
            P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName, charaWorld, currentLoginWorld), nameof(DCChange.OpenContextMenuForChara), TaskSettings.Timeout5M);
            P.TaskManager.Enqueue(DCChange.SelectReturnToHomeWorld);
            P.TaskManager.Enqueue(DCChange.ConfirmDcVisit, TaskSettings.Timeout2M);
            P.TaskManager.Enqueue(() => DCChange.ConfirmDcVisit2(null, null, 0, 0, tasks), "ConfirmDCVisit2", TaskSettings.Timeout2M);
        }
        tasks();
        P.TaskManager.Enqueue(DCChange.SelectOk, TaskSettings.TimeoutInfinite);
        P.TaskManager.Enqueue(() => DCChange.SelectServiceAccount(Utils.GetServiceAccount(charaName, charaWorld)), $"SelectServiceAccount_{charaName}@{charaWorld}", TaskSettings.Timeout1M);
    }
}
