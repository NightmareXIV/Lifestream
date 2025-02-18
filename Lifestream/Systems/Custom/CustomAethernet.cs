using Dalamud.Game.ClientState.Objects.Types;
using ECommons.ExcelServices;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;
using Lifestream.Systems.Residential;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Systems.Custom;
/// <summary>
/// For when sheets contain so little information or it's so much pain in the ass to get that it's just easier to define everything here.
/// </summary>
public sealed class CustomAethernet
{
    public static readonly uint BaseFirmamentId = 69420000;
    public static readonly uint BaseEurekaId = 69420100;
    public static readonly uint BaseBozjaId = 69420200;
    public static readonly uint BaseZandorId = 69420300;

    public readonly uint[] QuasiAethernetZones = [920, 975, 732];

    public readonly Dictionary<uint, float> MaxDistance = new()
    {
        [920] = 4.6f,
        [975] = 4.6f,
        [732] = 4.6f,
        [886] = 4.56f,
    };

    public readonly Dictionary<uint, List<CustomAetheryte>> ZoneInfo = new()
    {
        [920] = [
            new(new(-202.0f, 847.0f), 920, GetPlaceName(3529), BaseBozjaId),
            new(new(486.8f, 531.3f), 920, GetPlaceName(3530), BaseBozjaId+1),
            new(new(-258.0f, 534.4f), 920, GetPlaceName(3531), BaseBozjaId+2),
            new(new(169.8f, 192.3f), 920, GetPlaceName(3575), BaseBozjaId+3),
        ],
        [975] = [
            new(new(679.7f, 660.0f), 975, GetPlaceName(3664), BaseZandorId),
            new(new(-356.5f, 758.4f), 975, GetPlaceName(3665), BaseZandorId+1),
            new(new(-689.4f, -292.2f), 975, GetPlaceName(3666), BaseZandorId+2),
            new(new(106.4f, -130.8f), 975, GetPlaceName(3667), BaseZandorId+3),
        ],
        [886] = [
            new(new(23.9f, 169.4f), 886, GetPlaceName(3436), BaseFirmamentId), //The Mendicant's Court
            new(new(76.0f, 10.3f), 886, GetPlaceName(3473), BaseFirmamentId+1), //The Mattock
            new(new(149.5f, 98.6f), 886, GetPlaceName(3475), BaseFirmamentId+2), //The New Nest
            new(new(207.8f, -25.6f), 886, GetPlaceName(3474), BaseFirmamentId+3), //Saint Roelle's Dais
            new(new(-78.8f, 76.0f), 886, GetPlaceName(3525), BaseFirmamentId+4), //Featherfall
            new(new(-132.6f, -14.7f), 886, GetPlaceName(3528), BaseFirmamentId+5), //Hoarfrost Hall
            new(new(-91.7f, -115.2f), 886, GetPlaceName(3646), BaseFirmamentId+6), //The Risensong Quarter
            new(new(114.3f, -107.4f), 886, GetPlaceName(3645), BaseFirmamentId+7), //The Risensong Quarter
        ],
        [732] = [ //eureka anemos
            new(new(-138.9f, 543.2f), 732, GetPlaceName(2415), BaseEurekaId), //Port Surgate (2415),  (0), 
            new(new(-372.1f, -458.7f), 732, GetPlaceName(2429), BaseEurekaId+1), //Klauser's Peace (2421), Abandoned Laboratory (2429), 
            new(new(435.5f, -48.1f), 732, GetPlaceName(2436), BaseEurekaId+2), //The Val River Swale (2419), Windtorn Cabin (2436), 
        ]
    };

    public static string GetPlaceName(uint id)
    {
        return Svc.Data.GetExcelSheet<PlaceName>().GetRow(id).Name.GetText();
    }

    public CustomAetheryte? ActiveAetheryte = null;

    public void Tick()
    {
        if(Svc.ClientState.LocalPlayer != null && ZoneInfo.ContainsKey(P.Territory))
        {
            UpdateActiveAetheryte();
        }
        else
        {
            ActiveAetheryte = null;
        }
    }

    private void UpdateActiveAetheryte()
    {
        var a = Utils.GetValidAetheryte();
        if(a != null)
        {
            var aetheryte = GetFromIGameObject(a);
            if(aetheryte != null)
            {
                if(ActiveAetheryte == null)
                {
                    P.Overlay.IsOpen = true;
                }
                ActiveAetheryte = aetheryte;
            }
        }
        else
        {
            ActiveAetheryte = null;
        }
    }

    public CustomAetheryte? GetFromIGameObject(IGameObject obj)
    {
        if(obj == null) return null;
        var pos2 = obj.Position.ToVector2();
        if(ZoneInfo.TryGetValue(P.Territory, out var result))
        {
            foreach(var l in result)
            {
                if(Vector2.Distance(l.Position, pos2) < 10f)
                {
                    return l;
                }
            }
        }
        return null;
    }
}
