using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Systems.Custom;
public struct CustomAetheryte : IAetheryte, IEquatable<CustomAetheryte>
{
    public Vector2 Position { get; set; }
    public uint TerritoryType { get; set; }
    public string Name { get; set; }
    public uint ID { get; set; }

    public CustomAetheryte()
    {
    }

    public CustomAetheryte(Vector2 position, uint territoryType, string name, uint iD)
    {
        Position = position;
        TerritoryType = territoryType;
        Name = name;
        ID = iD;
    }

    public override bool Equals(object obj)
    {
        return obj is CustomAetheryte aetheryte && Equals(aetheryte);
    }

    public bool Equals(CustomAetheryte other)
    {
        return Position.Equals(other.Position) &&
               TerritoryType == other.TerritoryType &&
               Name == other.Name &&
               ID == other.ID;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, TerritoryType, Name, ID);
    }

    public static bool operator ==(CustomAetheryte left, CustomAetheryte right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CustomAetheryte left, CustomAetheryte right)
    {
        return !(left == right);
    }
}
