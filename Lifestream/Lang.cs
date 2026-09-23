using Dalamud.Utility;
using Lifestream.Enums;
using Lumina.Excel.Sheets;
using System.Text.RegularExpressions;

namespace Lifestream;

internal static class Lang
{
    public const string SymbolWard = "";
    public const string SymbolPlot = "";
    public const string SymbolApartment = "";
    public const string SymbolSubdivision = "";
    public static readonly (string Normal, string GameFont) Digits = ("0123456789", "");

    public static readonly string Help = $"""
    -- Main Travel --

    /li → go to your home world
    /li <world> → go to specified world
    /li <datacenter> → go to a random world of specified data center
    /li <aethernet> → go to specified aethernet destination
    /li <world>, tp <location> → go to specified aetheryte destination of specified world
    /li <world>, tp <aethernet> → go to specified aethernet destination of specified world

    -- Market Board --

    /li mb → go to market board
    /li <world> mb → go to market board of specified world

    -- Estates --

    /li auto → go to your private estate, shared estate, FC estate or apartment, based on configured preference
    /li shared → go to your shared estate, based on configured preference
    /li home → go to your private estate, alias: /li home|house|private
    /li fc → go to your FC estate, alias: /li fc|free|company|free company
    /li apt → go to your apartment, alias: /li apt|apartment
    /li ws → go to your FC's workshop, alias: /li ws|workshop

    /li <district> <ward> <plot> → go to specified plot in current world
    /li <world> <district> <ward> <plot> → go to specified plot of specified world
    Examples: /li lavender 1 30, /li goblet 1 30, /li mist 1 30

    -- Grand Company --

    /li gc → go to your grand company, alias: /li gc|hcc
    /li gc <grandcompany> → go to specified grand company, alias: /li gc|hcc <grandcompany>
    /li gcc → go to your grand company city's FC chest, alias: /li gc|hcc
    /li gcc <grandcompany> → go to specified grand company city's FC chest, alias: /li gc|hcc <grandcompany>
    Using "hc" or "hcc" instead of "gc" or "gcc" moves to you to your home world first

    -- Others --

    /li cosmic → go to Cosmic Exploration area, alias: /li cosmic|moon|ardorum
    /li island → go to Island Sanctuary
    /li firmament → - go to Firmamnent
    /li w → open world travel window, alias: /li w|world|open|select
    /lifestream → open plugin configuration 
    """;
    internal static string[] AdditionalChambersEntrance =>
    [
        Svc.Data.GetExcelSheet<EObjName>().GetRow(2004353).Singular.GetText(),
        Regex.Replace(Svc.Data.GetExcelSheet<EObjName>().GetRow(2004353).Singular.GetText(), @"\[.*?\]", "")
    ];

    internal static string[] EnterWorkshop => [GameText.Dialogue("custom/001/CmnDefHousingPersonalRoomEntrance_00178", 11)];

    internal static Dictionary<WorldChangeAetheryte, string> WorldChangeAetherytes = new()
    {
        [WorldChangeAetheryte.Gridania] = "New Gridania",
        [WorldChangeAetheryte.Uldah] = "Ul'Dah - Steps of Thal",
        [WorldChangeAetheryte.Limsa] = "Limsa Lominsa Lower Decks (not recommended)"
    };

    internal static class Symbols
    {
        internal const string HomeWorld = "";
        internal const string HighQuality = "";
    }

    internal static GameTextPattern LogInPartialText => new(
        Svc.Data.GetExcelSheet<Lobby>().GetRow(25).Text,
        Svc.Data.GetExcelSheet<Lobby>().GetRow(95).Text,
        Svc.Data.GetExcelSheet<Lobby>().GetRow(96).Text,
        Svc.Data.GetExcelSheet<Lobby>().GetRow(629).Text);

    internal static string[] Aethernet => [GameText.Dialogue("transport/Aetheryte", 1)];
    internal static string[] VisitAnotherWorld => [GameText.Dialogue("transport/Aetheryte", 3)];
    internal static GameTextPattern ConfirmWorldVisit => new(
        Svc.Data.GetExcelSheet<Addon>().GetRow(12519).Text,
        Svc.Data.GetExcelSheet<Addon>().GetRow(12624).Text);

    internal static string AethernetShard => Svc.Data.GetExcelSheet<EObjName>().GetRow(2000151).Singular.ToDalamudString().GetText();
    internal static string[] TravelToFirmament => [GameText.Dialogue("transport/AetheryteIshgard", 0)];
    public static string[] ResidentialDistrict => [GameText.Dialogue("transport/Aetheryte", 2)];
    public static string[] GoToWard => [Svc.Data.GetExcelSheet<Addon>().GetRow(6349).Text.GetText().Trim()];
    public static GameTextPattern TravelTo => new(Svc.Data.GetExcelSheet<Addon>().GetRow(6355).Text);

    public static string[] GoToSpecifiedApartment => [GameText.Dialogue("custom/003/HouFixMansionEntrance_00359", 2)];
    public static GameTextPattern EnterApartmenr => new(
        Svc.Data.GetExcelSheet<Addon>().GetRow(6782).Text,
        Svc.Data.GetExcelSheet<Addon>().GetRow(6784).Text);
    public static string[] GoToMyApartment => [GameText.Dialogue("custom/003/HouFixMansionEntrance_00359", 0)];

    public static string[] TravelToInstancedArea => [GameText.Dialogue("transport/Aetheryte", 16)];
    public static string ToReduceCongestion => Svc.Data.GetExcelSheet<Addon>().GetRow(2090).Text.GetText();
    public static string[] TravelToYourIsland => [GameText.Dialogue("custom/007/CtsMjiEntrance_00798", 4)];
    public static string[] TravelToMyIsland => [GameText.Dialogue("custom/007/CtsMjiEntrance_00798", 7)];
    public static string[] Entrance =>
    [
        Svc.Data.GetExcelSheet<EObjName>().GetRow(2002737).Singular.GetText(),
        Svc.Data.GetExcelSheet<EObjName>().GetRow(2002739).Singular.GetText()
    ];
    public static string[] ConfirmHouseEntrance => [Svc.Data.GetExcelSheet<Warp>().GetRow(131148).Question.GetText()];

    public static GameTextPattern UnableToSelectWorldForDcv => new(Svc.Data.GetExcelSheet<Lobby>().GetRow(1218).Text);

    public static readonly string[] RemainingSubTime = ["sqex.to/Msp"];

    public static readonly string AdjoiningArea = Svc.Data.GetExcelSheet<PlaceName>().GetRow(1252).Name.GetText();
    public static readonly string ToUpperLevel = Svc.Data.GetExcelSheet<PlaceName>().GetRow(1250).Name.GetText();
    public static readonly string ToLowerLevel = Svc.Data.GetExcelSheet<PlaceName>().GetRow(1251).Name.GetText();
    public static readonly string AethernetShardTooltip = Svc.Data.GetExcelSheet<PlaceName>().GetRow(1300).Name.GetText();
}
