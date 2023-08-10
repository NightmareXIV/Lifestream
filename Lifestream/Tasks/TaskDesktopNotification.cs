using Dalamud.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks
{
    internal unsafe static class TaskDesktopNotification
    {
        internal static void Enqueue(string s)
        {
            P.TaskManager.Enqueue(() =>
            {
                if (CSFramework.Instance()->WindowInactive)
                {
                    Util.TryNotify(s);
                }
            }, "TaskNotify");
        }
    }
}
