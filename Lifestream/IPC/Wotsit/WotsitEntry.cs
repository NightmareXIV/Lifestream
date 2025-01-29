using Lifestream.Tasks.SameWorld;

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
