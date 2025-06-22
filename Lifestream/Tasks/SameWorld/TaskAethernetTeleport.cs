using Lifestream.Schedulers;
using Lifestream.Systems.Legacy;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskAethernetTeleport
{
    internal static void Enqueue(TinyAetheryte a)
    {
        if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
        if(S.Data.DataStore.Aetherytes.ContainsKey(P.ActiveAetheryte.Value)) P.TaskManager.Enqueue(WorldChange.SelectAethernet);
        P.TaskManager.EnqueueDelay(C.SlowTeleport ? C.SlowTeleportThrottle : 0);
        P.TaskManager.Enqueue(() => WorldChange.TeleportToAethernetDestination(a.Name), nameof(WorldChange.TeleportToAethernetDestination));
    }

    internal static void Enqueue(string destination)
    {
        if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
        P.TaskManager.EnqueueDelay(C.SlowTeleport ? C.SlowTeleportThrottle : 0);
        P.TaskManager.Enqueue(() => WorldChange.TeleportToAethernetDestination(destination), nameof(WorldChange.TeleportToAethernetDestination));
    }
}
