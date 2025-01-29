using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Lifestream.Schedulers;
using OtterGui;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskAetheryteAethernetTeleport
{
    // Special values for the firmament.
    private const uint FirmamentRootAetheryteId = 70;
    internal const uint FirmamentAethernetId = uint.MaxValue;
    private const uint FirmamentRootAetheryteTerritoryId = 418;
    private const string Firmament = "The Firmament";

    internal static void Enqueue(uint rootAetheryteId, uint aethernetId)
    {
        if(aethernetId == FirmamentAethernetId)
        {
            if(rootAetheryteId != FirmamentRootAetheryteId)
            {
                throw new Exception($"Special firmament aethernet {FirmamentAethernetId} must be teleported from root aetheryte {FirmamentRootAetheryteId}");
            }
            EnqueueInner(FirmamentRootAetheryteId, FirmamentRootAetheryteTerritoryId, Firmament);
            return;
        }

        if(!P.DataStore.Aetherytes.Keys.FindFirst(a => a.ID == rootAetheryteId, out var rootAetheryte))
        {
            throw new Exception($"Root aetheryte {rootAetheryteId} not found");
        }
        if(!P.DataStore.Aetherytes[rootAetheryte].FindFirst(a => a.ID == aethernetId, out var aethernet))
        {
            throw new Exception($"Aethernet {aethernetId} not found under root aetheryte {rootAetheryteId}");
        }

        EnqueueInner(rootAetheryte.ID, rootAetheryte.TerritoryType, aethernet.Name);
    }

    private static void EnqueueInner(uint rootAetheryteId, uint territoryId, string aethernetName)
    {
        if(!Player.Available)
        {
            return;
        }

        DuoLog.Information($"Teleporting to {aethernetName}");
        TaskRemoveAfkStatus.Enqueue();

        // Teleport to the root aetheryte unless we're already close to it.
        P.TaskManager.Enqueue(() =>
        {
            if(Svc.ClientState.TerritoryType != territoryId || Utils.GetReachableAetheryte(x => Utils.TryGetTinyAetheryteFromIGameObject(x, out var ae) && ae.HasValue && ae.Value.ID == rootAetheryteId) == null)
            {
                P.TaskManager.InsertMulti(
                    new(() => S.TeleportService.TeleportToAetheryte(rootAetheryteId), "TeleportToRootAetheryte"),
                    new(Utils.WaitForScreenFalse),
                    new(Utils.WaitForScreen)
                    );
            }
        }, "ConditionalTeleportToRootAetheryte");

        // Target and ensure we're in range to interact.
        P.TaskManager.EnqueueDelay(10, true);
        P.TaskManager.Enqueue(WorldChange.TargetReachableMasterAetheryte);
        P.TaskManager.Enqueue(() =>
        {
            if(P.ActiveAetheryte == null)
            {
                P.TaskManager.InsertMulti(
                    new(WorldChange.LockOn),
                    new(WorldChange.EnableAutomove),
                    new(WorldChange.WaitUntilMasterAetheryteExists),
                    new(WorldChange.DisableAutomove),
                    new FrameDelayTask(10)
                    );
            }
        }, "ConditionalLockonTask");
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);

        // If we're going to the firmament, select the firmament option.
        if(aethernetName == Firmament)
        {
            P.TaskManager.Enqueue(() => Utils.TrySelectSpecificEntry(Lang.TravelToFirmament, () => EzThrottler.Throttle("SelectString")),
                "SelectTravelToFirmament");
            return;
        }

        // Otherwise, open the aethernet menu and select the destination.
        P.TaskManager.Enqueue(WorldChange.SelectAethernet);
        P.TaskManager.EnqueueDelay(P.Config.SlowTeleport ? P.Config.SlowTeleportThrottle : 0);
        P.TaskManager.Enqueue(() => WorldChange.TeleportToAethernetDestination(aethernetName),
            nameof(WorldChange.TeleportToAethernetDestination));
    }
}