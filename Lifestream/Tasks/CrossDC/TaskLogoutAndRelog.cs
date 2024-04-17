using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;

internal static class TaskLogoutAndRelog
{
    internal static void Enqueue(string nameWithWorld)
    {
        P.TaskManager.Enqueue(DCChange.WaitUntilNotBusy, new(timeLimitMS: 1.Minutes()));
        if (P.Config.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(DCChange.Logout);
        P.TaskManager.Enqueue(DCChange.SelectYesLogout, new(timeLimitMS: 1.Minutes()));
        P.TaskManager.Enqueue(DCChange.WaitUntilCanAutoLogin, new(timeLimitMS: 2.Minutes()));
        P.TaskManager.Enqueue(DCChange.TitleScreenClickStart, new(timeLimitMS: 1.Minutes()));
    }
}
