using ECommons.GameHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks;

internal static class TaskWaitUntilInWorld
{
    internal static void Enqueue(string world)
    {
        P.TaskManager.Enqueue(() =>
        {
            if (Player.Available && Player.CurrentWorld == world)
            {
                return true;
            }
            return false;
        }, 60.Minutes(), nameof(TaskWaitUntilInWorld));
    }
}
