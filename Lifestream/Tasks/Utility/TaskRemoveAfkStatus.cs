using ECommons.Automation;
using ECommons.GameHelpers;
using ECommons.Throttlers;

namespace Lifestream.Tasks;

internal static class TaskRemoveAfkStatus
{
    internal static void Enqueue()
    {
        P.TaskManager.Enqueue(() =>
        {
            if(Player.Object.OnlineStatus.Id == 17)
            {
                if (EzThrottler.Throttle("RemoveAfk"))
                {
                    Chat.Instance.SendMessage("/afk off");
                    return true;
                }
            }
            else
            {
                return true;
            }
            return false;
        }, "Remove afk");
        P.TaskManager.Enqueue(() => Player.Object.OnlineStatus.Id != 17, "WaitUntilNotAfk");
    }
}
