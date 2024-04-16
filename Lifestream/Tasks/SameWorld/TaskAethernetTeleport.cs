using Lifestream.Schedulers;
using Lifestream.Systems;
using Lifestream.Systems.Legacy;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskAethernetTeleport
{
    internal static void Enqueue(TinyAetheryte a)
    {
        if (P.Config.WaitForScreen) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
        if (P.DataStore.Aetherytes.ContainsKey(P.ActiveAetheryte.Value)) P.TaskManager.Enqueue(WorldChange.SelectAethernet);
        P.TaskManager.EnqueueDelay(P.Config.SlowTeleport ? P.Config.SlowTeleportThrottle : 0);
        P.TaskManager.Enqueue(() => WorldChange.TeleportToAethernetDestination(a), nameof(WorldChange.TeleportToAethernetDestination));
    }

    internal static void Enqueue(string destination)
    {
        if (P.Config.WaitForScreen) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
        P.TaskManager.EnqueueDelay(P.Config.SlowTeleport ? P.Config.SlowTeleportThrottle : 0);
        P.TaskManager.Enqueue(() => WorldChange.TeleportToAethernetDestination(destination), nameof(WorldChange.TeleportToAethernetDestination));
    }
}
