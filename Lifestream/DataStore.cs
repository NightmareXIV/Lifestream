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
                        Aetherytes[x.GetTinyAetheryte()] = new();
                        terr.Add(x.Territory.Value.RowId);
                        if(!StaticData.Data.ContainsKey(x.RowId))
                        {
                            StaticData.Data[x.RowId] = 0;
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
                        var a = x.GetTinyAetheryte();
                        Aetherytes[GetMaster(x)].Add(a);
                        terr.Add(x.Territory.Value.RowId);
                        if (!StaticData.Data.ContainsKey(x.RowId))
                        {
                            StaticData.Data[x.RowId] = 0;
                        }
                    }
                }
            });
            Territories = terr.ToArray();
        }

        internal void BuildWorlds()
        {
            BuildWorlds(Svc.ClientState.LocalPlayer.CurrentWorld.GameData.DataCenter.Value.RowId);
        }

        internal void BuildWorlds(uint dc)
        {
            Worlds = Svc.Data.GetExcelSheet<World>().Where(x => x.DataCenter.Value.RowId == dc && x.IsPublic).Select(x => x.Name.ToString()).ToArray();
        }

        /*internal bool TryGetAetherytes(uint TerritoryType, out Aetheryte Master, out List<Aetheryte> Slaves)
        {
            if(MasterAetherytes.TryGetValue(TerritoryType, out Master)) 
            {
                Slaves = SlaveAetherytes[Master.RowId];
                return true;
            }
            Master = default;
            Slaves = default;
            return false;
        }*/
    }
}
