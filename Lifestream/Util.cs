using ECommons.ExcelServices.TerritoryEnumeration;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal static unsafe class Util
    {
        internal static TinyAetheryte GetTinyAetheryte(this Aetheryte aetheryte)
        {
            var map = Svc.Data.GetExcelSheet<Map>().FirstOrDefault(m => m.TerritoryType.Row == aetheryte.Territory.Value.RowId);
            var scale = map.SizeFactor;
            var mapMarker = Svc.Data.GetExcelSheet<MapMarker>().FirstOrDefault(m => (m.DataType == (aetheryte.IsAetheryte? 3:4) && m.DataKey == (aetheryte.IsAetheryte?aetheryte.RowId:aetheryte.AethernetName.Value.RowId)));
            if(mapMarker == null)
            {
                PluginLog.Warning($"mapMarker is null for {aetheryte.RowId} {aetheryte.AethernetName.Value.Name}");
                return new(Vector2.Zero, aetheryte.Territory.Value.RowId, aetheryte.RowId, aetheryte.AethernetGroup);
            }
            var AethersX = ConvertMapMarkerToRawPosition(mapMarker.X, scale);
            var AethersY = ConvertMapMarkerToRawPosition(mapMarker.Y, scale);
            return new(new(AethersX, AethersY), aetheryte.Territory.Value.RowId, aetheryte.RowId, aetheryte.AethernetGroup);
        }
        
        internal static bool IsWorldChangeAetheryte(this TinyAetheryte t)
        {
            return t.ID.EqualsAny<uint>(2, 8, 9);
        }

        internal static float ConvertMapMarkerToMapCoordinate(int pos, float scale)
        {
            float num = scale / 100f;
            var rawPosition = (int)((float)(pos - 1024.0) / num * 1000f);
            return ConvertRawPositionToMapCoordinate(rawPosition, scale);
        }

        internal static float ConvertMapMarkerToRawPosition(int pos, float scale)
        {
            float num = scale / 100f;
            var rawPosition = ((float)(pos - 1024.0) / num);
            return rawPosition;
        }

        internal static float ConvertRawPositionToMapCoordinate(int pos, float scale)
        {
            float num = scale / 100f;
            return (float)((pos / 1000f * num + 1024.0) / 2048.0 * 41.0 / num + 1.0);
        }
    }
}
