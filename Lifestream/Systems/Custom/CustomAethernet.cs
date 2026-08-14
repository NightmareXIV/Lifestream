using Dalamud.Game.ClientState.Objects.Types;
using ECommons.MathHelpers;
using Lifestream.Data;
using Lumina.Excel.Sheets;

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
    private static uint BaseOccultId = 69420400;
    private static uint BaseMoonId = 69420500;

    public IEnumerable<uint> QuasiAethernetZones => ZoneInfo.Keys;

    public readonly Dictionary<uint, ZoneDetail> ZoneInfo = new()
    {
        [920] = new([
            new(new(-202.0f, 847.0f), 920, GetPlaceName(3529), BaseBozjaId),
            new(new(486.8f, 531.3f), 920, GetPlaceName(3530), BaseBozjaId+1),
            new(new(-258.0f, 534.4f), 920, GetPlaceName(3531), BaseBozjaId+2),
            new(new(169.8f, 192.3f), 920, GetPlaceName(3575), BaseBozjaId+3),
        ]),
        [975] = new([
            new(new(679.7f, 660.0f), 975, GetPlaceName(3664), BaseZandorId),
            new(new(-356.5f, 758.4f), 975, GetPlaceName(3665), BaseZandorId+1),
            new(new(-689.4f, -292.2f), 975, GetPlaceName(3666), BaseZandorId+2),
            new(new(106.4f, -130.8f), 975, GetPlaceName(3667), BaseZandorId+3),
        ]),
        [886] = new([
            new(new(23.9f, 169.4f), 886, GetPlaceName(3436), BaseFirmamentId), //The Mendicant's Court
            new(new(76.0f, 10.3f), 886, GetPlaceName(3473), BaseFirmamentId+1), //The Mattock
            new(new(149.5f, 98.6f), 886, GetPlaceName(3475), BaseFirmamentId+2), //The New Nest
            new(new(207.8f, -25.6f), 886, GetPlaceName(3474), BaseFirmamentId+3), //Saint Roelle's Dais
            new(new(-78.8f, 76.0f), 886, GetPlaceName(3525), BaseFirmamentId+4), //Featherfall
            new(new(-132.6f, -14.7f), 886, GetPlaceName(3528), BaseFirmamentId+5), //Hoarfrost Hall
            new(new(-91.7f, -115.2f), 886, GetPlaceName(3646), BaseFirmamentId+6), //The Risensong Quarter
            new(new(114.3f, -107.4f), 886, GetPlaceName(3645), BaseFirmamentId+7), //The Risensong Quarter
        ], 4.56f),
        [732] = new([ //eureka anemos
            new(new(-138.9f, 543.2f), 732, GetPlaceName(2415), BaseEurekaId), //Port Surgate (2415),  (0),
            new(new(-372.1f, -458.7f), 732, GetPlaceName(2429), BaseEurekaId+1), //Klauser's Peace (2421), Abandoned Laboratory (2429),
            new(new(435.5f, -48.1f), 732, GetPlaceName(2436), BaseEurekaId+2), //The Val River Swale (2419), Windtorn Cabin (2436),
        ]),
        [763] = new([ //eureka pagos
            new(new(-893.7f, 159.0f), 763, GetPlaceName(2463), BaseEurekaId+10), //Icepoint (2463),  (0),
            new(new(91.1f, 303.2f), 763, GetPlaceName(2474), BaseEurekaId+11), //Eureka Pagos (2462), Vlondette's Retreat (2474),
            new(new(-707.1f, -318.9f), 763, GetPlaceName(2472), BaseEurekaId+12), //Eureka Pagos (2462), Geothermal Studies (2472),
            new(new(346.0f, -289.7f), 763, GetPlaceName(2473), BaseEurekaId+13), //Eureka Pagos (2462), Gravitational Studies (2473),
            ]),
        [795] = new([ //eureka pyros
            new(new(-253.5f, 146.8f), 795, GetPlaceName(2531), BaseEurekaId+20), //Northpoint (2531),  (0),
            new(new(125.7f, 795.3f), 795, GetPlaceName(2540), BaseEurekaId+21), //Southwestern Ice Needles (2534), The Dragon Star Observatory (2540),
            new(new(127.9f, -196.1f), 795, GetPlaceName(2541), BaseEurekaId+22), //Bonfire (2536), The Firing Chamber (2541),
            new(new(-443.4f, -622.7f), 795, GetPlaceName(2542), BaseEurekaId+23), //West Flamerock (2537), Carbonatite Quarry (2542),
            ]),
        [827] = new([ //eureka hydatos
            new(new(-61.7f, -875.6f), 827, GetPlaceName(2876), BaseEurekaId+30), //Central Point (2876),  (0),
            new(new(-587.3f, -148.4f), 827, GetPlaceName(2891), BaseEurekaId+31), //The West Val River Bank (2877), Unverified Research (2891),
            new(new(781.1f, -417.5f), 827, GetPlaceName(2892), BaseEurekaId+32), //The East Val River Bank (2879), Dormitory (2892),
            ]),
        [1252] = new([
            new(new(830.7f, -696.0f), 1252, GetPlaceName(4944), BaseOccultId++), //Southdown Heath (4934), Expedition Base Camp (4944),
            new(new(-173.0f, -611.1f), 1252, GetPlaceName(4928), BaseOccultId++), //4936	The Wanderer's Haven	1	Wanderer's Haven	0	0	1	0	0		0	0	0
            new(new(-358.1f, -121.0f), 1252, GetPlaceName(4929), BaseOccultId++), //4939	Crystallized Caverns	1	crystallized caverns	0	0	1	0	0		0	0	0
            new(new(306.9f, 305.7f), 1252, GetPlaceName(4930), BaseOccultId++), //4940	Eldergrowth	1	Eldergrowth	0	0	1	0	0		0	0	0
            new(new(-384.1f, 281.4f), 1252, GetPlaceName(4947), BaseOccultId++), //4947	Stonemarsh	1	Stonemarsh	0	0	1	0	1		0	0	0
            ]),
        [1346] = new([
            new(new(880.0f, 880.1f), 1346, GetPlaceName(5571), BaseOccultId++),
            new(new(451.7f, 528.8f), 1346, GetPlaceName(5576), BaseOccultId++),
            new(new(357.7f, -554.3f), 1346, GetPlaceName(5572), BaseOccultId++),
            new(new(-547.2f, 594.4f), 1346, GetPlaceName(5573), BaseOccultId++),
            new(new(-388.6f, -440.5f), 1346, GetPlaceName(5574), BaseOccultId++),
            new(new(-13.7f, -40.5f), 1346, GetPlaceName(5575), BaseOccultId++),
            ]),
        [1237] = new([
            new(new(-3.8f, -32.2f), 1237, GetPlaceName(WKSAetheryte.Get(1).Name.RowId), BaseMoonId++), //The Cosmoor (5220), Moongate Hub (5225),
            new(new(-540.0f, -526.8f), 1237, GetPlaceName(WKSAetheryte.Get(2).Name.RowId), BaseMoonId++), //Calabash Cove (5221),  (0),
            new(new(427.2f, 497.5f), 1237, GetPlaceName(WKSAetheryte.Get(3).Name.RowId), BaseMoonId++), //Weddingway's Bower (5222),  (0),
            new(new(-619.6f, 399.8f), 1237, GetPlaceName(WKSAetheryte.Get(4).Name.RowId), BaseMoonId++), //Gleamslope (5223),  (0),
            new(new(629.8f, -572.8f), 1237, GetPlaceName(WKSAetheryte.Get(5).Name.RowId), BaseMoonId++), //Lunar Nadir (5224),  (0),
            ]),
        [1291] = new([
            new(new(336.1f, -381.8f), 1291, GetPlaceName(WKSAetheryte.Get(6).Name.RowId), BaseMoonId++), //Glassblown Grotto (5302), Glassblowers' Beacon (5316),
            new(new(-150.8f, 314.4f), 1291, GetPlaceName(WKSAetheryte.Get(7).Name.RowId), BaseMoonId++), //Iridized Rise (5306),  (0),
            new(new(-640.1f, -627.0f), 1291, GetPlaceName(WKSAetheryte.Get(8).Name.RowId), BaseMoonId++), //The Saltpeter Shore (5305),  (0),
            new(new(672.9f, 422.6f), 1291, GetPlaceName(WKSAetheryte.Get(9).Name.RowId), BaseMoonId++), //Capsule Chasm (5307),  (0),
            new(new(-590.9f, 722.2f), 1291, GetPlaceName(WKSAetheryte.Get(10).Name.RowId), BaseMoonId++), //Fusingway Vent (5308),  (0),
            ]),
        [1310] = new([
            new(new(-164.1f, 83.5f), 1310, GetPlaceName(WKSAetheryte.Get(11).Name.RowId), BaseMoonId++), //Megalithopolis (5407), Terra Firma (5420),
            new(new(-141.2f, -591.2f), 1310, GetPlaceName(WKSAetheryte.Get(12).Name.RowId), BaseMoonId++), //The Regolift (5409),  (0),

            new(new(-454.2f, 760.8f), 1310, GetPlaceName(WKSAetheryte.Get(13).Name.RowId), BaseMoonId++), //Cape Geras (5410),  (0),
            new(new(733.8f, -101.0f), 1310, GetPlaceName(WKSAetheryte.Get(14).Name.RowId), BaseMoonId++), //Shadefleet (5412),  (0),
            new(new(-124.6f, -801.5f), 1310, GetPlaceName(WKSAetheryte.Get(15).Name.RowId), BaseMoonId++), //Emerald Echoes (5413), Desert Burrow (5424, 5460),
            ]),
        [1319] = new([
            new(new(259.8f, 356.3f), 1319, GetPlaceName(WKSAetheryte.Get(16).Name.RowId), BaseMoonId++), //The Timberlodge
            new(new(-226.4f, -560.4f), 1319, GetPlaceName(WKSAetheryte.Get(17).Name.RowId), BaseMoonId++), //Full Bloom Gardens
            new(new(-242.7f, 321.2f), 1319, GetPlaceName(WKSAetheryte.Get(18).Name.RowId), BaseMoonId++), //Pileus Pergola
            new(new(-599.9f, -375.1f), 1319, GetPlaceName(WKSAetheryte.Get(19).Name.RowId), BaseMoonId++), //Sylvan Stacks
            new(new(-674.7f, 621.6f), 1319, GetPlaceName(WKSAetheryte.Get(20).Name.RowId), BaseMoonId++), //Stratostone Stand
        ])
    };

    public Dictionary<uint, string> CustomAetheryteNames
    {
        get
        {
            if (field == null)
            {
                field = [];
                foreach (var x in S.Data.CustomAethernet.ZoneInfo)
                {
                    foreach (var a in x.Value.Aetherytes)
                    {
                        field.Add(a.ID, a.Name);
                    }
                }
            }
            return field;
        }
    }

    public static string GetPlaceName(uint id)
    {
        return Svc.Data.GetExcelSheet<PlaceName>().GetRow(id).Name.GetText();
    }

    public CustomAetheryte? ActiveAetheryte = null;

    public void Tick()
    {
        if (Svc.ClientState.LocalPlayer != null && ZoneInfo.ContainsKey(P.Territory))
        {
            UpdateActiveAetheryte();
        }
        else
        {
            ActiveAetheryte = null;
        }
    }

    public void UpdateActiveAetheryte()
    {
        var a = Utils.GetValidAetheryte();
        if (a != null)
        {
            var aetheryte = GetFromIGameObject(a);
            if (aetheryte != null)
            {
                if (ActiveAetheryte == null)
                {
                    S.Gui.Overlay.IsOpen = true;
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
        if (obj == null)
            return null;
        var pos2 = obj.Position.ToVector2();
        if (ZoneInfo.TryGetValue(P.Territory, out var result))
        {
            foreach (var l in result.Aetherytes)
            {
                if (Vector2.Distance(l.Position, pos2) < 10f)
                {
                    return l;
                }
            }
        }
        return null;
    }
}
