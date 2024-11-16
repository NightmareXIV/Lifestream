using ECommons.ExcelServices;
using ECommons.GameHelpers;

namespace Lifestream.Tasks;

internal static class TaskWaitUntilInWorld
{
    internal static void Enqueue(string world, bool checkDc)
    {
        P.TaskManager.Enqueue(() => Task(world, checkDc), nameof(TaskWaitUntilInWorld), TaskSettings.TimeoutInfinite);
    }

    internal static bool Task(string world, bool checkDc)
    {
        if(checkDc)
        {
            if(Player.Available && ExcelWorldHelper.Get(world)?.DataCenter.RowId == Svc.ClientState.LocalPlayer.CurrentWorld.Value.DataCenter.RowId)
            {
                return true;
            }
        }
        if(Player.Available && Player.CurrentWorld == world)
        {
            return true;
        }
        return false;
    }
}
