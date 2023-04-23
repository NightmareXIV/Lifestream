using ECommons.Configuration;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Lifestream
{
    internal class DataStore
    {
        internal const string FileName = "StaticData.json";
        internal uint[] Territories;
        internal Dictionary<TinyAetheryte, List<TinyAetheryte>> Aetherytes = new();
        internal string[] Worlds = Array.Empty<string>();
        internal StaticData StaticData;

        internal TinyAetheryte GetMaster(Aetheryte aetheryte)
        {
            foreach(var x in Aetherytes.Keys)
            {
                if (x.Group == aetheryte.AethernetGroup) return x;
            }
            return default;
        }

        internal DataStore()
        {
            var terr = new List<uint>();
            StaticData = EzConfig.LoadConfiguration<StaticData>(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, FileName), false);
            Svc.Data.GetExcelSheet<Aetheryte>().Each(x =>
            {
                if (x.AethernetGroup != 0)
                {
                    if (x.IsAetheryte)
                    {
                        Aetherytes[GetTinyAetheryte(x)] = new();
                        terr.Add(x.Territory.Value.RowId);
                        if(!StaticData.Callback.ContainsKey(x.RowId))
                        {
                            StaticData.Callback[x.RowId] = 0;
                        }
                    }
                }
            });
            Svc.Data.GetExcelSheet<Aetheryte>().Each(x =>
            {
                if (x.AethernetGroup != 0)
                {
                    if (!x.IsAetheryte)
                    {
                        var a = GetTinyAetheryte(x);
                        Aetherytes[GetMaster(x)].Add(a);
                        terr.Add(x.Territory.Value.RowId);
                        if (!StaticData.Callback.ContainsKey(x.RowId))
                        {
                            StaticData.Callback[x.RowId] = 0;
                        }
                    }
                }
            });
            foreach(var x in Aetherytes.Keys.ToArray())
            {
                Aetherytes[x] = Aetherytes[x].OrderBy(x => GetAetheryteSortOrder(x.ID)).ToList();
            }
            Territories = terr.ToArray();
        }
        
        internal uint GetAetheryteSortOrder(uint id)
        {
            if(StaticData.SortOrder.TryGetValue(id, out var x))
            {
                return x;
            }
            return 0;
        }

        internal void BuildWorlds()
        {
            BuildWorlds(Svc.ClientState.LocalPlayer.CurrentWorld.GameData.DataCenter.Value.RowId);
        }

        internal void BuildWorlds(uint dc)
        {
            Worlds = Svc.Data.GetExcelSheet<World>().Where(x => x.DataCenter.Value.RowId == dc && x.IsPublic).Select(x => x.Name.ToString()).Order().ToArray();
        }

        internal TinyAetheryte GetTinyAetheryte(Aetheryte aetheryte)
        {
            var AethersX = 0f;
            var AethersY = 0f;
            if (StaticData.CustomPositions.TryGetValue(aetheryte.RowId, out var pos))
            {
                AethersX = pos.X;
                AethersY = pos.Z;
            }
            else
            {
                var map = Svc.Data.GetExcelSheet<Map>().FirstOrDefault(m => m.TerritoryType.Row == aetheryte.Territory.Value.RowId);
                var scale = map.SizeFactor;
                var mapMarker = Svc.Data.GetExcelSheet<MapMarker>().FirstOrDefault(m => (m.DataType == (aetheryte.IsAetheryte ? 3 : 4) && m.DataKey == (aetheryte.IsAetheryte ? aetheryte.RowId : aetheryte.AethernetName.Value.RowId)));
                if (mapMarker != null)
                {
                    AethersX = Util.ConvertMapMarkerToRawPosition(mapMarker.X, scale);
                    AethersY = Util.ConvertMapMarkerToRawPosition(mapMarker.Y, scale);
                }
            }
            return new(new(AethersX, AethersY), aetheryte.Territory.Value.RowId, aetheryte.RowId, aetheryte.AethernetGroup);
        }
    }
}
