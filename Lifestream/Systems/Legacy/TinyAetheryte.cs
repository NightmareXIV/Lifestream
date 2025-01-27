using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace Lifestream.Systems.Legacy;

public struct TinyAetheryte : IEquatable<TinyAetheryte>, IAetheryte
{
    public Vector2 Position { get; set; }
    public uint TerritoryType { get; set; }
    public uint ID { get; set; }
    public string Name { get; set; }
    public uint Group { get; set; }
    public bool IsAetheryte { get; set; }
    public bool Invisible { get; set; }
    private Aetheryte Ref { get; init; }

    public TinyAetheryte(Vector2 position, uint territoryType, uint iD, uint group)
    {
        Ref = Svc.Data.GetExcelSheet<Aetheryte>().GetRow(iD);
        Position = position;
        TerritoryType = territoryType;
        ID = iD;
        Group = group;
        Name = Ref.AethernetName.Value.Name.ToDalamudString().TextValue;
        IsAetheryte = Ref.IsAetheryte;
        Invisible = Ref.Invisible;
    }

    public override bool Equals(object obj)
    {
        return obj is TinyAetheryte aetheryte && Equals(aetheryte);
    }

    public bool Equals(TinyAetheryte other)
    {
        return Position.Equals(other.Position) &&
               TerritoryType == other.TerritoryType &&
               ID == other.ID &&
               Group == other.Group;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, TerritoryType, ID, Group);
    }

    public static bool operator ==(TinyAetheryte left, TinyAetheryte right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TinyAetheryte left, TinyAetheryte right)
    {
        return !(left == right);
    }
}
