using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal static class Lang
    {
        /*
        1	TEXT_AETHERYTE_TOWN_WARP<Gui(69)/> Aethernet.
        1	TEXT_AETHERYTE_TOWN_WARP<Gui(69)/> 都市転送網
        1	TEXT_AETHERYTE_TOWN_WARP<Gui(69)/> Ätheryten<SoftHyphen/> netz
        1	TEXT_AETHERYTE_TOWN_WARP<Gui(69)/> Réseau de transport urbain éthéré
        */
        internal static readonly string[] Aethernet = new string[] { "Aethernet.", "都市転送網", "Ätherytennetz", "Réseau de transport urbain éthéré" };

        /*
         * 3	TEXT_AETHERYTE_MENU_WKT	<Gui(69)/> Visit Another World Server.
            3	TEXT_AETHERYTE_MENU_WKT	<Gui(69)/> 他のワールドへ遊びにいく
            3	TEXT_AETHERYTE_MENU_WKT	<Gui(69)/> Weltenreise
            3	TEXT_AETHERYTE_MENU_WKT	<Gui(69)/> Voyager vers un autre Monde
         * */
        internal static readonly string[] VisitAnotherWorld = new string[] { "Visit Another World Server.", "他のワールドへ遊びにいく", "Weltenreise", "Voyager vers un autre Monde", };

        //2000151	Aethernet shard	0	Aethernet shards	0	1	1	0	0
        internal static string AethernetShard => Svc.Data.GetExcelSheet<EObjName>().GetRow(2000151).Singular.ToDalamudString().ExtractText();
    }
}
