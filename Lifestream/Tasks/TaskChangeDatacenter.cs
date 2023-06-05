using ECommons.GameHelpers;
using Lifestream.Schedulers;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks
{
    internal static class TaskChangeDatacenter
    {
        internal static void Enqueue(string destination, string charaName)
        {
            var dc = Svc.Data.GetExcelSheet<World>().First(x => x.Name == destination).DataCenter.Value.Name.ToString();
            PluginLog.Debug($"Beginning data center changing process. Destination: {dc}, {destination}");
            P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName), 5.Minutes());
            P.TaskManager.Enqueue(DCChange.SelectVisitAnotherDC);
            P.TaskManager.Enqueue(DCChange.ConfirmDcVisitIntention);
            P.TaskManager.Enqueue(() => DCChange.SelectTargetDataCenter(dc), 2.Minutes());
            P.TaskManager.Enqueue(() => DCChange.SelectTargetWorld(destination), 2.Minutes());
            P.TaskManager.Enqueue(DCChange.ConfirmDcVisit, 2.Minutes());
            P.TaskManager.Enqueue(DCChange.ConfirmDcVisit2, 2.Minutes());
            P.TaskManager.Enqueue(DCChange.SelectOk, int.MaxValue);
            TaskSelectChara.Enqueue(charaName);
        }
    }
}
