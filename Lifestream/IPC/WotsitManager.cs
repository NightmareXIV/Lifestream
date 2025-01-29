using Dalamud.Plugin.Ipc;
using Dalamud.Utility;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lifestream.Data;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.Sheets;
using GrandCompany = ECommons.ExcelServices.GrandCompany;

namespace Lifestream.IPC;

public class WotsitEntry
{
    public string DisplayName { get; init; }
    public string SearchString { get; init; }
    public uint IconId { get; init; }
    public Delegate Callback { get; init; }

    public static WotsitEntry AetheryteAethernetTeleport(string townName, string name, uint aetheryteId, uint aethernetId)
    {
        var searchStr = name + (townName != null ? $" - {townName}" : "");
        return new WotsitEntry
        {
            DisplayName = "Teleport to " + searchStr,
            SearchString = searchStr,
            IconId = 111,
            Callback = () => TaskAetheryteAethernetTeleport.Enqueue(aetheryteId, aethernetId),
        };
    }

    // Callback is intentionally not included in equality checks.
    public override int GetHashCode() => HashCode.Combine(DisplayName, SearchString, IconId);
    public override bool Equals(object obj) => obj is WotsitEntry entry && Equals(entry);
    public bool Equals(WotsitEntry other) => DisplayName == other.DisplayName && SearchString == other.SearchString && IconId == other.IconId;

    public override string ToString() => $"{GetType().Name}(\"{DisplayName}\", \"{SearchString}\", {IconId})";
}

public static class WotsitEntryGenerator
{
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

    // These values cannot be read sometimes during a territory change, so we
    // cache them permanently otherwise they will disappear.
    private static readonly HashSet<ulong> HasPrivateEstate = [];
    private static readonly HashSet<ulong> HasFreeCompanyEstate = [];
    private static readonly HashSet<ulong> HasApartment = [];

    public static IEnumerable<WotsitEntry> Generate()
    {
        var includes = P.Config.WotsitIntegrationIncludes;

        foreach (var entry in Generic(includes))
        {
            yield return entry;
        }

        // TODO: eventually residential aethernet could be added too
        if (includes.AetheryteAethernet)
        {
            foreach (var entry in AetheryteAethernetTeleport())
            {
                yield return entry;
            }
        }

        if (includes.AddressBook)
        {
            foreach (var entry in AddressBook())
            {
                yield return entry;
            }
        }

        if (includes.CustomAlias)
        {
            foreach (var entry in CustomAlias())
            {
                yield return entry;
            }
        }
    }

    private static IEnumerable<WotsitEntry> Generic(Config.WotsitIntegrationIncludedItems includes)
    {
        if (Player.CID != 0 && TaskPropertyShortcut.GetPrivateHouseAetheryteID() != 0)
        {
            HasPrivateEstate.Add(Player.CID);
        }
        if (Player.CID != 0 && TaskPropertyShortcut.GetFreeCompanyAetheryteID() != 0)
        {
            HasFreeCompanyEstate.Add(Player.CID);
        }
        if (Player.CID != 0 && TaskPropertyShortcut.GetApartmentAetheryteID().ID != 0)
        {
            HasApartment.Add(Player.CID);
        }

        if (includes.WorldSelect)
        {
            yield return new WotsitEntry
            {
                DisplayName = "Open world select window",
                SearchString = "open world select window - open travel window",
                IconId = 55,
                Callback = () => S.SelectWorldWindow.IsOpen = true,
            };
        }
        if (includes.PropertyAuto)
        {
            yield return new WotsitEntry
            {
                DisplayName = "Auto-teleport to property",
                SearchString = "auto-teleport to property",
                IconId = 52,
                Callback = () => TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Auto),
            };
        }
        if (includes.PropertyPrivate && HasPrivateEstate.Contains(Player.CID)) {
            yield return new WotsitEntry
            {
                DisplayName = "Teleport to your private estate",
                SearchString = "teleport to your private estate - teleport to home",
                IconId = 52,
                Callback = () => TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Home),
            };
        }
        if (includes.PropertyFreeCompany && HasFreeCompanyEstate.Contains(Player.CID))
        {
            yield return new WotsitEntry
            {
                DisplayName = "Teleport to your free company estate",
                SearchString = "teleport to your free company estate - teleport to fc",
                IconId = 52,
                Callback = () => TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.FC),
            };
        }
        if (includes.PropertyApartment && HasApartment.Contains(Player.CID)) {
            yield return new WotsitEntry
            {
                DisplayName = "Teleport to your apartment",
                SearchString = "teleport to your apartment",
                IconId = 52,
                Callback = () => TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Apartment),
            };
        }
        if (includes.PropertyInn)
        {
            // TODO: each inn could be a separate entry in the future
            yield return new WotsitEntry
            {
                DisplayName = "Teleport to an inn room",
                SearchString = "teleport to an inn room",
                IconId = 88,
                Callback = () => TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Inn),
            };
        }
        // TODO: each grand company could be a separate entry in the future
        if (includes.GrandCompany && Player.GrandCompany != GrandCompany.Unemployed)
        {
            var (name, icon) = Player.GrandCompany switch
            {
                GrandCompany.Maelstrom => ("Maelstrom", 60313u),
                GrandCompany.TwinAdder => ("Twin Adder", 60306u),
                GrandCompany.ImmortalFlames => ("Immortal Flames", 60305u),
                _ => throw new NotImplementedException(),
            };
            yield return new WotsitEntry
            {
                DisplayName = "Teleport to your grand company - " + name,
                SearchString = name + " - teleport to your grand company headquarters - teleport to gc hq",
                IconId = icon,
                Callback = () => TaskGCShortcut.Enqueue(Player.GrandCompany),
            };
        }
        if (includes.MarketBoard)
        {
            yield return new WotsitEntry
            {
                DisplayName = "Teleport to a market board",
                SearchString = "teleport to a market board - teleport to mb",
                IconId = 63,
                Callback = () => TaskMBShortcut.Enqueue(),
            };
        }
        // TODO: the NPCs on the island sanctuary could get their own entries
        if (includes.IslandSanctuary)
        {
            yield return new WotsitEntry
            {
                DisplayName = "Teleport to your island sanctuary",
                SearchString = "teleport to your island sanctuary - teleport to is",
                IconId = 23,
                Callback = () => TaskISShortcut.Enqueue(),
            };
        }
    }

    private static IEnumerable<WotsitEntry> AetheryteAethernetTeleport()
    {
        // Avoid spoilers by not showing aetherytes that the player hasn't
        // attuned to.
        uint[] visibleAetheryteIds = [];
        if (Player.Available)
        {
            unsafe
            {
                var visibleAetherytes = Telepo.Instance()->UpdateAetheryteList();
                if (visibleAetherytes != null)
                {
                    visibleAetheryteIds = visibleAetherytes->Select(x => x.AetheryteId).ToArray();
                }
            }
        }

        foreach (var (rootAetheryte, aethernetShards) in P.DataStore.Aetherytes)
        {
            if (visibleAetheryteIds.Length > 0 && !visibleAetheryteIds.Contains(rootAetheryte.ID))
            {
                PluginLog.Debug($"WotsitEntryGenerator.AetheryteAethernetTeleport: Skipping aetheryte {rootAetheryte.ID} ({rootAetheryte.Name}) because it is not visible");
                continue;
            }

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
                    yield return WotsitEntry.AetheryteAethernetTeleport(townName, name, rootAetheryte.ID, aethernetShard.ID);
                }
            }

            // Special case for The Firmament
            if (P.Config.Firmament && rootAetheryte.TerritoryType == 418)
            {
                var placeName = Svc.Data.GetExcelSheet<PlaceName>().GetRow(3435).Name.ToDalamudString().TextValue;
                yield return WotsitEntry.AetheryteAethernetTeleport(townName, placeName, rootAetheryte.ID, TaskAetheryteAethernetTeleport.FirmamentAethernetId);
            }
        }
    }

    private static IEnumerable<WotsitEntry> AddressBook()
    {
        foreach (var entry in P.Config.AddressBookFolders.SelectMany(folder => folder.Entries))
        {
            var searchStr = entry.Name + (!string.IsNullOrEmpty(entry.Alias) && entry.Alias != entry.Name ? " - " + entry.Alias : "");
            yield return new WotsitEntry
            {
                DisplayName = $"Teleport to address {entry.Name}",
                SearchString = searchStr,
                IconId = 52,
                Callback = entry.GoTo,
            };
        }
    }

    private static IEnumerable<WotsitEntry> CustomAlias()
    {
        foreach (var alias in P.Config.CustomAliases.Where(a => a.Enabled && !string.IsNullOrEmpty(a.Alias)))
        {
            yield return new WotsitEntry
            {
                DisplayName = $"Run alias {alias.Alias}",
                SearchString = alias.Alias,
                IconId = 55,
                Callback = () => alias.Enqueue(),
            };
        }
    }
}

public class WotsitManager : IDisposable
{
    // Map from registered Guid string to the entry.
    private readonly Dictionary<string, WotsitEntry> _registered = new();
    private HashSet<WotsitEntry> _lastEntries = new();

    private readonly ICallGateSubscriber<bool> _faAvailable;
    private readonly ICallGateSubscriber<string, bool> _faInvoke;
    private ICallGateSubscriber<string, string, string, uint, string>? _faRegisterWithSearch;

    public WotsitManager()
    {
        _faAvailable = Svc.PluginInterface.GetIpcSubscriber<bool>("FA.Available");
        _faAvailable.Subscribe(OnAvailable);
        _faInvoke = Svc.PluginInterface.GetIpcSubscriber<string, bool>("FA.Invoke");
        _faInvoke.Subscribe(HandleInvoke);
        // To handle (re)logins (and plugin reloads).
        ProperOnLogin.RegisterAvailable(OnLogin, true);
        // To handle logouts and clear all entries.
        Svc.ClientState.Logout += OnLogout;
        // To catch new config options, address book entries, aliases, etc.
        EzConfig.OnSave += ConfigSaved;
        // To periodically reload generated entries.
        Svc.ClientState.TerritoryChanged += TerritoryChanged;
    }

    public void Dispose()
    {
        ClearWotsit();
        _faAvailable?.Unsubscribe(OnAvailable);
        _faInvoke?.Unsubscribe(HandleInvoke);
        ProperOnLogin.Unregister(OnLogin);
        Svc.ClientState.Logout -= OnLogout;
        EzConfig.OnSave -= ConfigSaved;
        Svc.ClientState.TerritoryChanged -= TerritoryChanged;
        GC.SuppressFinalize(this);
    }

    private void HandleInvoke(string id)
    {
        if (!_registered.TryGetValue(id, out var entry))
        {
            return;
        }

        PluginLog.Debug($"WotsitManager: Received FA.Invoke(\"{id}\") => {entry.DisplayName}");
        try
        {
            entry.Callback.DynamicInvoke();
        }
        catch (Exception e)
        {
            DuoLog.Error($"WotsitManager: Could not handle FA.Invoke(\"{id}\") ({entry.DisplayName}): {e}");
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
        _lastEntries = [];
        var faUnregisterAll = Svc.PluginInterface.GetIpcSubscriber<string, bool>("FA.UnregisterAll");
        faUnregisterAll!.InvokeFunc(P.Name);
        PluginLog.Debug($"WotsitManager: Invoked FA.UnregisterAll(\"{P.Name}\")");
        _registered.Clear();
    }

    private void OnAvailable()
    {
        PluginLog.Debug("WotsitManager: FA.Available triggered, forcing re-registration");
        MaybeTryInit(true);
    }

    private void OnLogin()
    {
        PluginLog.Debug("WotsitManager: ProperOnLogin.Available triggered, forcing re-registration");
        MaybeTryInit(true);
    }

    private void OnLogout(int type, int code)
    {
        PluginLog.Debug("WotsitManager: ClientState.Logout triggered, clearing wotsit");
        TryClearWotsit();
    }

    private void ConfigSaved()
    {
        if (!Player.Available)
        {
            return;
        }
        PluginLog.Debug("WotsitManager: Config saved, attempting re-registration");
        MaybeTryInit(false);
    }

    private void TerritoryChanged(ushort territory)
    {
        PluginLog.Debug($"WotsitManager: Territory changed to {territory}, attempting re-registration");
        MaybeTryInit(false);
    }

    public void MaybeTryInit(bool force = false)
    {
        if (!P.Config.WotsitIntegrationEnabled)
        {
            return;
        }

        try
        {
            Init(force);
        }
        catch (Exception e)
        {
            PluginLog.Warning($"WotsitManager: Failed to initialize: {e}");
        }
    }

    private void Init(bool force = false)
    {
        var newEntries = WotsitEntryGenerator.Generate().ToHashSet();
        if (!force && _lastEntries.Count != 0 && newEntries.SetEquals(_lastEntries))
        {
            PluginLog.Debug("WotsitManager: Entries have not changed, skipping re-registration");
            return;
        }
#if DEBUG
        // Log the actual differences for debugging.
        if (_lastEntries.Count > 0)
        {
            foreach (var added in newEntries.Except(_lastEntries).ToArray())
            {
                PluginLog.Debug($"WotsitManager: New entry: {added}");
            }
            foreach (var removed in _lastEntries.Except(newEntries).ToArray())
            {
                PluginLog.Debug($"WotsitManager: Removed entry: {removed}");
            }
        }
#endif
        ClearWotsit();
        _lastEntries = newEntries;

        _faRegisterWithSearch = Svc.PluginInterface.GetIpcSubscriber<string, string, string, uint, string>("FA.RegisterWithSearch");

        foreach (var entry in newEntries)
        {
            var id = _faRegisterWithSearch!.InvokeFunc(P.Name, entry.DisplayName, $"{P.Name} {entry.SearchString}", entry.IconId);
            _registered.Add(id, entry);
            PluginLog.Debug($"WotsitManager: Invoked FA.RegisterWithSearch(\"{P.Name}\", \"{entry.DisplayName}\", \"{entry.SearchString}\", {entry.IconId}) => {id}");
        }
    }
}
