using Lifestream.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.CrossDC;
public static class TaskLogout
{
    public static void Enqueue()
    {
        P.TaskManager.Enqueue(DCChange.WaitUntilNotBusy, TaskSettings.Timeout1M);
        if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(DCChange.Logout);
        P.TaskManager.Enqueue(DCChange.SelectYesLogout, TaskSettings.Timeout1M);
        P.TaskManager.Enqueue(DCChange.WaitUntilCanAutoLogin, TaskSettings.Timeout2M);
    }
}