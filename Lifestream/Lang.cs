﻿using Dalamud.Utility;
using Lifestream.Enums;
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
        internal static string[] TravelToFirmament = ["Travel to the Firmament.", "蒼天街転送", "Himmelsstadt", "Azurée"];
    }
}
