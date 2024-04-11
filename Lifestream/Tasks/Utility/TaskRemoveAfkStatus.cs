using ECommons.Automation;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
