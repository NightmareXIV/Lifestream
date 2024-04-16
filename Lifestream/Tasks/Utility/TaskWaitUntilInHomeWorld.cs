using ECommons.GameHelpers;
using Lifestream.Schedulers;

namespace Lifestream.Tasks;

internal static class TaskWaitUntilInHomeWorld
{
    internal static void Enqueue()
    {
        P.TaskManager.Enqueue(() => Player.Available && Player.IsInHomeWorld, "Waiting until player returns to home world", new(timeLimitMS:60.Minutes()));
        P.TaskManager.Enqueue(DCChange.WaitUntilNotBusy, "Waiting until player is not busy (TaskWaitUntilInHomeWorld)", new(timeLimitMS: 1.Minutes()));
    }
}
