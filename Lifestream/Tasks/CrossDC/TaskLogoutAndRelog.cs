using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;

internal static class TaskLogoutAndRelog
{
    internal static void Enqueue(string nameWithWorld)
    {
        P.TaskManager.Enqueue(DCChange.WaitUntilNotBusy, TaskSettings.Timeout1M);
        if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(DCChange.Logout);
        P.TaskManager.Enqueue(DCChange.SelectYesLogout, TaskSettings.Timeout1M);
        P.TaskManager.Enqueue(DCChange.WaitUntilCanAutoLogin, TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(DCChange.TitleScreenClickStart, TaskSettings.Timeout1M);
    }
}
