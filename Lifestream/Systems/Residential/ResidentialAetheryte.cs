using Lumina.Excel.Sheets;

namespace Lifestream.Systems.Residential;
public struct ResidentialAetheryte : IEquatable<ResidentialAetheryte>, IAetheryte
{
    public Vector2 Position { get; set; }
    public uint TerritoryType { get; set; }
    public string Name { get; set; }
    public uint ID { get; set; }
    public bool IsSubdivision;
    private HousingAethernet Ref;

    public ResidentialAetheryte(HousingAethernet data, bool isSubdivision, Vector2 subdivisionPositionModifier)
    {
        Ref = data;
        Name = data.PlaceName.Value.Name.GetText();
        TerritoryType = data.TerritoryType.RowId;
        ID = data.RowId;
        IsSubdivision = isSubdivision;
        Position = GetCoordinates();
        if(isSubdivision) Position += subdivisionPositionModifier;
    }

    public override bool Equals(object obj)
    {
        return obj is ResidentialAetheryte aetheryte && Equals(aetheryte);
    }

    public bool Equals(ResidentialAetheryte other)
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

    private readonly Vector2 GetCoordinates()
    {
        var AethersX = 0f;
        var AethersY = 0f;
        var reference = this;
        {
            var map = Svc.Data.GetExcelSheet<Map>().FirstOrDefault(m => m.TerritoryType.RowId == reference.Ref.TerritoryType.RowId);
            var scale = map.SizeFactor;
            if(Svc.Data.GetSubrowExcelSheet<MapMarker>().AllRows().TryGetFirst(m => m.DataType == 4 && m.DataKey.RowId == reference.Ref.PlaceName.RowId, out var mapMarker))
            {
                AethersX = Utils.ConvertMapMarkerToRawPosition(mapMarker.X, scale);
                AethersY = Utils.ConvertMapMarkerToRawPosition(mapMarker.Y, scale);
            }
        }
        return new(AethersX, AethersY);
    }

    public static bool operator ==(ResidentialAetheryte left, ResidentialAetheryte right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ResidentialAetheryte left, ResidentialAetheryte right)
    {
        return !(left == right);
    }
}
