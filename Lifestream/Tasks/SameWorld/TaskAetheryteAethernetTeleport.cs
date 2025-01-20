using Dalamud.Utility;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Lifestream.Schedulers;
using Lumina.Excel.Sheets;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskAetheryteAethernetTeleport
{
    // Special values for the firmament.
    internal const uint FirmamentRootAetheryteId = 70;
    internal const uint FirmamentAethernetId = uint.MaxValue;
    private const uint FirmamentRootAetheryteTerritoryId = 418;
    private const string Firmament = "The Firmament";

    internal static void Enqueue(uint rootAetheryteId, uint aethernetId)
    {
        if (aethernetId == FirmamentAethernetId)
        {
            if (rootAetheryteId != FirmamentRootAetheryteId)
            {
                throw new Exception($"Special firmament aethernet {FirmamentAethernetId} must be teleported from root aetheryte {FirmamentRootAetheryteId}");
            }
            EnqueueInner(FirmamentRootAetheryteId, FirmamentRootAetheryteTerritoryId, Firmament);
            return;
        }

        var rootAetheryte = Svc.Data.GetExcelSheet<Aetheryte>().GetRow(rootAetheryteId);
        var aethernet = Svc.Data.GetExcelSheet<Aetheryte>().GetRow(aethernetId);
        if (!rootAetheryte.IsAetheryte)
        {
            throw new Exception($"Root aetheryte {rootAetheryteId} is not a full aetheryte");
        }
        if (rootAetheryte.AethernetGroup == 0)
        {
            throw new Exception($"Root aetheryte {rootAetheryteId} is not part of an aethernet group");
        }
        if (aethernet.IsAetheryte)
        {
            throw new Exception($"Aethernet {aethernetId} is not an aethernet shard");
        }
        if (rootAetheryte.AethernetGroup != aethernet.AethernetGroup)
        {
            throw new Exception($"Aethernet {aethernetId} is not in the same aethernet network as root aetheryte {rootAetheryteId}");
        }

        EnqueueInner(rootAetheryte.RowId, rootAetheryte.Territory.RowId, aethernet.AethernetName.Value.Name.ToDalamudString().TextValue);
    }

    private static void EnqueueInner(uint rootAetheryteId, uint territoryId, string aethernetName)
    {
        if (!Player.Available)
        {
            return;
        }

        TaskRemoveAfkStatus.Enqueue();

        // Teleport to the root aetheryte unless we're already close to it.
        if (Svc.ClientState.TerritoryType != territoryId || Utils.GetReachableAetheryte(x => Utils.TryGetTinyAetheryteFromIGameObject(x, out var ae) && ae.HasValue && ae.Value.ID == rootAetheryteId) == null)
        {
            P.TaskManager.Enqueue(() => S.TeleportService.TeleportToAetheryte(rootAetheryteId), "TeleportToRootAetheryte");
            P.TaskManager.Enqueue(Utils.WaitForScreenFalse);
            P.TaskManager.Enqueue(Utils.WaitForScreen);
        }

        // Target and ensure we're in range to interact.
        P.TaskManager.EnqueueDelay(10, true);
        P.TaskManager.Enqueue(WorldChange.TargetReachableMasterAetheryte);
        P.TaskManager.Enqueue(() =>
        {
            if (P.ActiveAetheryte == null)
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
        if (aethernetName == Firmament)
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