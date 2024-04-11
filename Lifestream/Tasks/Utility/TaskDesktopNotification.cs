namespace Lifestream.Tasks;

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
