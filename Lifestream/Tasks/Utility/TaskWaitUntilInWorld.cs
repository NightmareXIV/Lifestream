using ECommons.GameHelpers;

namespace Lifestream.Tasks;

internal static class TaskWaitUntilInWorld
{
    internal static void Enqueue(string world)
    {
        P.TaskManager.Enqueue(() =>
        {
            if (Player.Available && Player.CurrentWorld == world)
            {
                return true;
            }
            return false;
        }, nameof(TaskWaitUntilInWorld), TaskSettings.TimeoutInfinite);
    }
}
