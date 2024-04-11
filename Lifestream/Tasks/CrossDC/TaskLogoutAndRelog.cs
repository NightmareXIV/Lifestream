using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;

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
