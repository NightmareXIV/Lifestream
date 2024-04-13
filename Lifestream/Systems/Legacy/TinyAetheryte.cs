using Lumina.Excel.GeneratedSheets;

namespace Lifestream.Systems.Legacy;

public struct TinyAetheryte : IEquatable<TinyAetheryte>, IAetheryte
{
    public Vector2 Position { get; set; }
    public uint TerritoryType { get; set; }
    public uint ID { get; set; }
    public string Name { get; set; }
    public uint Group;
    public bool IsAetheryte;
    private Aetheryte Ref { get; init; }

    public TinyAetheryte(Vector2 position, uint territoryType, uint iD, uint group)
    {
        Ref = Svc.Data.GetExcelSheet<Aetheryte>().GetRow(iD);
        Position = position;
        TerritoryType = territoryType;
        ID = iD;
        Group = group;
        Name = Ref.AethernetName.Value.Name.ToString();
        IsAetheryte = Ref.IsAetheryte;
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
