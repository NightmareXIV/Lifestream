namespace Lifestream.Tasks;

internal static unsafe class TaskDesktopNotification
{
    internal static void Enqueue(string s)
    {
        P.TaskManager.Enqueue(() =>
        {
            if(CSFramework.Instance()->WindowInactive)
            {
                Utils.TryNotify(s);
            }
        }, "TaskNotify");
    }
}
