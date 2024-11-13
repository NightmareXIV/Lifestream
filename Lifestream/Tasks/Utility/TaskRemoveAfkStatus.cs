using ECommons.Automation;
using ECommons.GameHelpers;
using ECommons.Throttlers;

namespace Lifestream.Tasks;

internal static class TaskRemoveAfkStatus
{
    public static readonly ConditionFlag[] MoveCancelConditions = [ConditionFlag.InThatPosition];

    internal static void Enqueue()
    {
        P.TaskManager.Enqueue(() =>
        {
            if(Player.Object.OnlineStatus.RowId == 17)
            {
                if(EzThrottler.Throttle("RemoveAfk"))
                {
                    Chat.Instance.SendMessage("/afk off");
                    P.TaskManager.InsertTask(new(() => Player.Object.OnlineStatus.RowId != 17, "WaitUntilNotAfk"));
                }
            }
            if(MoveCancelConditions.Select(x => Svc.Condition[x]).Any(x => x))
            {
                P.TaskManager.InsertMulti(
                    new(() => Chat.Instance.ExecuteCommand("/automove on"), "Enable automove (AntiEmote)"),
                    new(() => Chat.Instance.ExecuteCommand("/automove off"), "Disable automove (AntiEmote)"),
                    new(() => !MoveCancelConditions.Select(x => Svc.Condition[x]).Any(x => x), "WaitUntilNotEmoting")
                    );
            }
            return true;
        }, "Remove afk/busy status");
    }
}
