using Dalamud.Utility;
using Lifestream.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks
{
    internal static class TaskLogoutAndRelog
    {
        internal static void Enqueue(string nameWithWorld)
        {
            P.TaskManager.Enqueue(DCChange.WaitUntilNotBusy, 1.Minutes());
            P.TaskManager.Enqueue(DCChange.Logout);
            P.TaskManager.Enqueue(DCChange.SelectYesLogout, 1.Minutes());
            P.TaskManager.Enqueue(DCChange.WaitUntilCanAutoLogin, 2.Minutes());
            P.TaskManager.Enqueue(DCChange.TitleScreenClickStart, 1.Minutes());
        }
    }
}
