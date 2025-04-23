using ECommons.GameHelpers;
using ECommons.Throttlers;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskFirmanentTeleport
{
    internal static void Enqueue()
    {
        if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
        P.TaskManager.Enqueue(() =>
        {
            if(!Player.Available) return false;
            return Utils.TrySelectSpecificEntry(Lang.TravelToFirmament, () => EzThrottler.Throttle("SelectString"));
        }, $"TeleportToFirmamentSelect {Lang.TravelToFirmament}");
    }
}
