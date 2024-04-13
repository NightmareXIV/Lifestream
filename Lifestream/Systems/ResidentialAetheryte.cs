using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Systems;
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
        Name = data.PlaceName.Value.Name.ExtractText();
        TerritoryType = data.TerritoryType.Row;
        ID = data.RowId;
        IsSubdivision = isSubdivision;
        Position = GetCoordinates();
        if (isSubdivision) Position += subdivisionPositionModifier;
    }

    public override bool Equals(object obj)
    {
        return obj is ResidentialAetheryte aetheryte && this.Equals(aetheryte);
    }

    public bool Equals(ResidentialAetheryte other)
    {
        return this.Position.Equals(other.Position) &&
               this.TerritoryType == other.TerritoryType &&
               this.Name == other.Name &&
               this.ID == other.ID;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Position, this.TerritoryType, this.Name, this.ID);
    }

    private readonly Vector2 GetCoordinates()
    {
        var AethersX = 0f;
        var AethersY = 0f;
        var reference = this;
        {
            var map = Svc.Data.GetExcelSheet<Map>().FirstOrDefault(m => m.TerritoryType.Row == reference.Ref.TerritoryType.Row);
            var scale = map.SizeFactor;
            var mapMarker = Svc.Data.GetExcelSheet<MapMarker>().FirstOrDefault(m => m.DataType == 4 && m.DataKey == reference.Ref.PlaceName.Row);
            if (mapMarker != null)
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
