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
            var dc = Svc.Data.GetExcelSheet<World>().First(x => x.Name == destination).Name.ToString();
            P.TaskManager.Enqueue(DCChange.WaitUntilNotBusy);
            P.TaskManager.Enqueue(DCChange.Logout);
            P.TaskManager.Enqueue(DCChange.SelectYesLogout);
            P.TaskManager.Enqueue(DCChange.Logout, 60 * 1000);
            P.TaskManager.Enqueue(DCChange.TitleScreenClickStart);
            P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName), 300 * 1000);
            P.TaskManager.Enqueue(DCChange.SelectVisitAnotherDC);
            P.TaskManager.Enqueue(DCChange.ConfirmDcVisitIntention);
            P.TaskManager.Enqueue(() => DCChange.SelectTargetDataCenter(dc), 60 * 1000);
            P.TaskManager.Enqueue(DCChange.ConfirmDcVisit, 60 * 1000);
            P.TaskManager.Enqueue(DCChange.ConfirmDcVisit2, 60 * 1000);
            P.TaskManager.Enqueue(DCChange.SelectOk, int.MaxValue);
            P.TaskManager.Enqueue(() => DCChange.SelectCharacter(charaName));
        }
    }
}
