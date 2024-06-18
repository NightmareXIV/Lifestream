using Dalamud.Utility;
using Lifestream.Enums;
using Lumina.Excel.GeneratedSheets;

namespace Lifestream;

internal static class Lang
{
    public const string SymbolWard = "";
    public const string SymbolPlot = "";
    public const string SymbolApartment = "";
    public const string SymbolSubdivision = "";
		public static readonly (string Normal, string GameFont) Digits = ("0123456789", "");


		internal static Dictionary<WorldChangeAetheryte, string> WorldChangeAetherytes = new()
    {
        [WorldChangeAetheryte.Gridania] = "New Gridania",
        [WorldChangeAetheryte.Uldah] = "Ul'Dah - Steps of Thal",
        [WorldChangeAetheryte.Limsa] = "Limsa Lominsa Lower Decks (not recommended)"
    };

    internal static class Symbols
    {
        internal const string HomeWorld = "";
    }

    internal static readonly string[] LogInPartialText = ["Logging in with", "Log in with", "でログインします。", "einloggen?", "eingeloggt.", "Se connecter avec", "Vous allez vous connecter avec", "Souhaitez-vous vous connecter avec"];

    /*
    1	TEXT_AETHERYTE_TOWN_WARP<Gui(69)/> Aethernet.
    1	TEXT_AETHERYTE_TOWN_WARP<Gui(69)/> 都市転送網
    1	TEXT_AETHERYTE_TOWN_WARP<Gui(69)/> Ätheryten<SoftHyphen/> netz
    1	TEXT_AETHERYTE_TOWN_WARP<Gui(69)/> Réseau de transport urbain éthéré
    */
    internal static readonly string[] Aethernet = ["Aethernet.", "都市転送網", "Ätherytennetz", "Réseau de transport urbain éthéré"];

    /*
     * 3	TEXT_AETHERYTE_MENU_WKT	<Gui(69)/> Visit Another World Server.
        3	TEXT_AETHERYTE_MENU_WKT	<Gui(69)/> 他のワールドへ遊びにいく
        3	TEXT_AETHERYTE_MENU_WKT	<Gui(69)/> Weltenreise
        3	TEXT_AETHERYTE_MENU_WKT	<Gui(69)/> Voyager vers un autre Monde
     * */
    internal static readonly string[] VisitAnotherWorld = ["Visit Another World Server.", "他のワールドへ遊びにいく", "Weltenreise", "Voyager vers un autre Monde",];

    internal static readonly string[] ConfirmWorldVisit = ["Travel to", "へ移動します、よろしいですか？", "reisen?", "Voulez-vous vraiment visiter"];

    //2000151	Aethernet shard	0	Aethernet shards	0	1	1	0	0
    internal static string AethernetShard => Svc.Data.GetExcelSheet<EObjName>().GetRow(2000151).Singular.ToDalamudString().ExtractText();

    //0	TEXT_AETHERYTEISHGARD_HWD_WARP	<Gui(69)/> Travel to the Firmament.
    //0	TEXT_AETHERYTEISHGARD_HWD_WARP	<Gui(69)/> 蒼天街転送
    //0	TEXT_AETHERYTEISHGARD_HWD_WARP<Gui(69)/> Himmelsstadt
    //0	TEXT_AETHERYTEISHGARD_HWD_WARP	<Gui(69)/> Azurée
    internal static readonly string[] TravelToFirmament = ["Travel to the Firmament.", "蒼天街転送", "Himmelsstadt", "Azurée"];

    //2	TEXT_AETHERYTE_HOUSING_WARP	<Gui(69)/> Residential District Aethernet.
    //2	TEXT_AETHERYTE_HOUSING_WARP	<Gui(69)/> 冒険者居住区転送
    //2	TEXT_AETHERYTE_HOUSING_WARP	<Gui(69)/> Wohngebiet
    //2	TEXT_AETHERYTE_HOUSING_WARP	<Gui(69)/> Quartier résidentiel
    public static readonly string[] ResidentialDistrict = ["Residential District Aethernet.", "冒険者居住区転送", "Wohngebiet", "Quartier résidentiel"];
    public static readonly string[] GoToWard = ["Go to specified ward. (Review Tabs)", "区を指定して移動（ハウスアピール確認）", "Zum angegebenen Bezirk (Zweck der Unterkunft einsehen)", "Spécifier le secteur où aller (Voir les attraits)"];

    //6355	<Sheet(PlaceName,IntegerParameter(1),0)/>第<Value>IntegerParameter(2)</Value>区に移動します。
    //よろしいですか？
    //6355	Zu Wohnbezirk <Value>IntegerParameter(2)</Value> <Sheet(PlaceName,IntegerParameter(1),8,2)/> gehen?
    //6355	Vous allez vous rendre à “<Sheet(PlaceName,IntegerParameter(1),0)/>” dans le secteur <Value>IntegerParameter(2)</Value>. Confirmer<Indent/>?
    public static readonly string[] TravelTo = ["Travel to", "よろしいですか？", "Zu Wohnbezirk", "Vous allez vous rendre à"];

    public static readonly string[] GoToSpecifiedApartment = ["Go to specified apartment", "Go to speciz`fied apartment", "部屋番号を指定して移動（ハウスアピール確認）", "Eine bestimmte Wohnung betreten (Zweck der Unterkunft einsehen)"];
    public static readonly string[] EnterApartmenr = ["Enter", "よろしいですか？", "betreten?", "Aller dans l'appartement"];
    public static readonly string[] GoToMyApartment = ["Go to your apartment"];
}
