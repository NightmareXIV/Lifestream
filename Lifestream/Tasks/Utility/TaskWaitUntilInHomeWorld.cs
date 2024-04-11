using ECommons.GameHelpers;
using Lifestream.Schedulers;

namespace Lifestream.Tasks;

internal static class TaskWaitUntilInHomeWorld
{
    internal static void Enqueue()
    {
        P.TaskManager.Enqueue(() => Player.Available && Player.IsInHomeWorld, 60.Minutes(), "Waiting until player returns to home world");
        P.TaskManager.Enqueue(DCChange.WaitUntilNotBusy, 1.Minutes(), "Waiting until player is not busy (TaskWaitUntilInHomeWorld)");
    }
}
