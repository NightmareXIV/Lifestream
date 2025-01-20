using Dalamud.Plugin.Ipc;
using Dalamud.Utility;
using Lifestream.Tasks.SameWorld;
using Lumina.Excel.Sheets;

namespace Lifestream.IPC;

public class WotsitManager : IDisposable
{
    // Map from registered Guid string to (aetheryte ID, aethernet ID).
    private readonly Dictionary<string, (uint, uint)> _registered = new();

    private readonly ICallGateSubscriber<bool> _faAvailable;
    private readonly ICallGateSubscriber<string, bool> _faInvoke;
    private ICallGateSubscriber<string, string, string, uint, string>? _faRegisterWithSearch;

    private static readonly Dictionary<uint, uint> AetheryteToTownPlaceName = new()
    {
        { 2, 39 },     // Gridania
        { 8, 27 },     // Limsa Lominsa
        { 9, 51 },     // Ul'dah
        { 62, 1484 },  // The Gold Saucer
        { 70, 62 },    // Ishgard
        { 75, 2082 },  // Idyllshire
        { 104, 2403 }, // Rhalgr's Reach
        { 111, 513 },  // Kugane
        { 127, 2507 }, // The Doman Enclave
        { 133, 516 },  // The Crystarium
        { 134, 517 },  // Eulmore
        { 182, 3706 }, // Old Sharlayan
        { 183, 3707 }, // Radz-at-Han
        { 216, 4504 }, // Tuliyollal
        { 217, 4503 }, // Solution Nine
    };

    // Any invisible aethernet shards that should be added to wotsit. In the
    // future this should be replaced with a config option.
    private static readonly List<uint> InvisibleWhitelist = [
        91,  // Prologue Gate (Western Hinterlands)
        92,  // Epilogue Gate (Eastern Hinterlands)
        120, // The Ruby Price
    ];

    public WotsitManager()
    {
        _faAvailable = Svc.PluginInterface.GetIpcSubscriber<bool>("FA.Available");
        _faAvailable.Subscribe(MaybeTryInit);
        _faInvoke = Svc.PluginInterface.GetIpcSubscriber<string, bool>("FA.Invoke");
        _faInvoke.Subscribe(HandleInvoke);
        MaybeTryInit();
    }

    public void Dispose()
    {
        ClearWotsit();
        _faAvailable?.Unsubscribe(MaybeTryInit);
        _faInvoke?.Unsubscribe(HandleInvoke);
        GC.SuppressFinalize(this);
    }

    private void HandleInvoke(string id)
    {
        if (!_registered.TryGetValue(id, out var value))
        {
            return;
        }
        var (aetheryteId, aethernetId) = value;
        PluginLog.Debug($"WotsitManager: Received FA.Invoke(\"{id}\") => ({aetheryteId}, {aethernetId})");
        try
        {
            TaskAetheryteAethernetTeleport.Enqueue(aetheryteId, aethernetId);
        }
        catch (Exception e)
        {
            DuoLog.Error($"Could not teleport to aethernet ({aetheryteId}, {aethernetId}): {e}");
        }
    }

    public void TryClearWotsit()
    {
        try
        {
            ClearWotsit();
        }
        catch (Exception e)
        {
            PluginLog.Warning($"WotsitManager: Failed to clear wotsit: {e}");
        }
    }

    private void ClearWotsit()
    {
        var faUnregisterAll = Svc.PluginInterface.GetIpcSubscriber<string, bool>("FA.UnregisterAll");
        faUnregisterAll!.InvokeFunc(P.Name);
        PluginLog.Debug($"WotsitManager: Invoked FA.UnregisterAll(\"{P.Name}\")");
        _registered.Clear();
    }

    public void MaybeTryInit()
    {
        if (!P.Config.WotsitIntegrationEnabled)
        {
            return;
        }

        try
        {
            Init();
        }
        catch (Exception e)
        {
            PluginLog.Warning($"WotsitManager: Failed to initialize: {e}");
        }
    }

    private void Init()
    {
        ClearWotsit();

        _faRegisterWithSearch = Svc.PluginInterface.GetIpcSubscriber<string, string, string, uint, string>("FA.RegisterWithSearch");

        // TODO: filter out unavailable aetherytes (unless this already does??)
        foreach (var (rootAetheryte, aethernetShards) in P.DataStore.Aetherytes)
        {
            string townName = null;
            if (AetheryteToTownPlaceName.TryGetValue(rootAetheryte.ID, out var placeId))
            {
                townName = Svc.Data.GetExcelSheet<PlaceName>().GetRow(placeId).Name.ToDalamudString().TextValue;
            }
            foreach (var aethernetShard in aethernetShards)
            {
                if (!P.Config.Hidden.Contains(aethernetShard.ID) && (!aethernetShard.Invisible || InvisibleWhitelist.Contains(aethernetShard.ID)))
                {
                    var name = P.Config.Renames.TryGetValue(aethernetShard.ID, out var value) ? value : aethernetShard.Name;
                    AddWotsitEntry(townName, name, rootAetheryte.ID, aethernetShard.ID);
                }
            }

            // Special case for The Firmament
            if (P.Config.Firmament && rootAetheryte.TerritoryType == 418)
            {
                var placeName = Svc.Data.GetExcelSheet<PlaceName>().GetRow(3435).Name.ToDalamudString().TextValue;
                AddWotsitEntry(townName, placeName, rootAetheryte.ID, TaskAetheryteAethernetTeleport.FirmamentAethernetId);
            }
        }
    }

    private void AddWotsitEntry(string townName, string name, uint aetheryteId, uint aethernetId)
    {
        var searchStr = name + (townName != null ? $" - {townName}" : "");
        var displayName = "Teleport to " + name;

        // TODO: icon ID
        var id = _faRegisterWithSearch!.InvokeFunc(P.Name, displayName, searchStr, 0);
        _registered.Add(id, (aetheryteId, aethernetId));
        PluginLog.Debug($"WotsitManager: Invoked FA.RegisterWithSearch(\"{P.Name}\", \"{displayName}\", \"{searchStr}\", 0) => {id} => ({aetheryteId}, {aethernetId})");
    }
}
