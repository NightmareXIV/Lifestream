using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;

internal static class TaskLogoutAndRelog
{
    internal static void Enqueue(string nameWithWorld)
    {
        TaskLogout.Enqueue();
        P.TaskManager.Enqueue(DCChange.TitleScreenClickStart, TaskSettings.Timeout1M);
    }
}
