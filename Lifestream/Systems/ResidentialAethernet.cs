using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.MathHelpers;
using Lifestream.GUI;
using Lifestream.Systems.Legacy;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Systems;
public sealed class ResidentialAethernet : IDisposable
{
    public readonly Dictionary<uint, ResidentialZoneInfo> ZoneInfo = new() {
        [ResidentalAreas.The_Goblet] = new() { SubdivisionModifier = new(-700f, -700f)},
        [ResidentalAreas.Mist] = new() { SubdivisionModifier = new(-700f, -700f) },
        [ResidentalAreas.The_Lavender_Beds] = new() { SubdivisionModifier = new(-700f, -700f) },
        [ResidentalAreas.Empyreum] = new() { SubdivisionModifier = new(-704f, -654f) },
        [ResidentalAreas.Shirogane] = new() { SubdivisionModifier = new(-700f, -700f) },
    };

    public ResidentialAetheryte? ActiveAetheryte = null;

    public bool IsInResidentialZone() => ZoneInfo.ContainsKey(Svc.ClientState.TerritoryType);

    public ResidentialAethernet()
    {
        try
        {
            foreach (var zone in ZoneInfo)
            {
                var values = Svc.Data.GetExcelSheet<HousingAethernet>().Where(a => a.TerritoryType.Row == zone.Key).OrderBy(x => x.Order);
                foreach (var a in values)
                {
                    var aetheryte = new ResidentialAetheryte(a, a.Order >= values.Count() / 2, zone.Value.SubdivisionModifier);
                    zone.Value.Aetherytes.Add(aetheryte);
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void Dispose()
    {
        
    }

    public void Tick()
    {
        if (Svc.ClientState.LocalPlayer != null && ZoneInfo.ContainsKey(Svc.ClientState.TerritoryType))
        {
            UpdateActiveAetheryte();
        }
        else
        {
            ActiveAetheryte = null;
        }
    }

    void UpdateActiveAetheryte()
    {
        var a = Utils.GetValidAetheryte();
        if (a != null)
        {
            var pos2 = a.Position.ToVector2();
            if(ZoneInfo.TryGetValue(Svc.ClientState.TerritoryType, out var result))
            {
                foreach (var l in result.Aetherytes)
                {
                    if (Vector2.Distance(l.Position, pos2) < 10)
                    {
                        if (ActiveAetheryte == null)
                        {
                            P.Overlay.IsOpen = true;
                        }
                        ActiveAetheryte = l;
                        return;
                    }
                }
            }
        }
        else
        {
            ActiveAetheryte = null;
        }
    }
}
