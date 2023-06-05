using Lifestream.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks
{
    internal class TaskReturnToHomeDC
    {
        internal static void Enqueue(string charaName)
        {
            PluginLog.Debug($"Beginning returning home process.");
            P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName), 5.Minutes(), nameof(DCChange.OpenContextMenuForChara));
            P.TaskManager.Enqueue(DCChange.SelectReturnToHomeWorld);
            P.TaskManager.Enqueue(DCChange.ConfirmDcVisit, 2.Minutes());
            P.TaskManager.Enqueue(DCChange.ConfirmDcVisit2, 2.Minutes());
            P.TaskManager.Enqueue(DCChange.SelectOk, int.MaxValue);
        }
    }
}
