using ECommons.Automation.NeoTaskManager;
using Lifestream.Enums;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskTpToAethernetDestination
{
    internal static void Enqueue(WorldChangeAetheryte worldChangeAetheryte)
        => P.TaskManager.EnqueueMulti(BuildTasks(worldChangeAetheryte));

    internal static void Insert(WorldChangeAetheryte worldChangeAetheryte)
        => P.TaskManager.InsertMulti(BuildTasks(worldChangeAetheryte));

    private static TaskManagerTask[] BuildTasks(WorldChangeAetheryte worldChangeAetheryte)
    {
        var tasks = new List<TaskManagerTask>();

        if (C.WaitForScreenReady)
        {
            tasks.Add(new(Utils.WaitForScreen));
        }

        tasks.Add(new(() => WorldChange.ExecuteTPToAethernetDestination((uint)worldChangeAetheryte)));
        tasks.Add(new(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas"));
        tasks.Add(new(WorldChange.WaitUntilNotBusy, TaskSettings.Timeout2M));
        tasks.Add(new(() => Player.Interactable && P.Territory == worldChangeAetheryte.GetTerritory(), "WaitUntilPlayerInteractable", TaskSettings.Timeout2M));

        return tasks.ToArray();
    }
}