using ClickLib.Clicks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Memory;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Enums;
using Lifestream.Systems.Legacy;
using Lumina.Excel.GeneratedSheets;
using CharaData = (string Name, ushort World);

namespace Lifestream;

internal static unsafe class Utils
{
    public static uint[] AethernetShards = [2000151, 2000153, 2000154, 2000155, 2000156, 2000157, 2003395, 2003396, 2003397, 2003398, 2003399, 2003400, 2003401, 2003402, 2003403, 2003404, 2003405, 2003406, 2003407, 2003408, 2003409, 2003995, 2003996, 2003997, 2003998, 2003999, 2004000, 2004968, 2004969, 2004970, 2004971, 2004972, 2004973, 2004974, 2004976, 2004977, 2004978, 2004979, 2004980, 2004981, 2004982, 2004983, 2004984, 2004985, 2004986, 2004987, 2004988, 2004989, 2007434, 2007435, 2007436, 2007437, 2007438, 2007439, 2007855, 2007856, 2007857, 2007858, 2007859, 2007860, 2007861, 2007862, 2007863, 2007864, 2007865, 2007866, 2007867, 2007868, 2007869, 2007870, 2009421, 2009432, 2009433, 2009562, 2009563, 2009564, 2009565, 2009615, 2009616, 2009617, 2009618, 2009713, 2009714, 2009715, 2009981, 2010135, 2011142, 2011162, 2011163, 2011241, 2011243, 2011373, 2011374, 2011384, 2011385, 2011386, 2011387, 2011388, 2011389, 2011573, 2011574, 2011575, 2011677, 2011678, 2011679, 2011680, 2011681, 2011682, 2011683, 2011684, 2011685, 2011686, 2011687, 2011688, 2011689, 2011690, 2011691, 2011692, 2012252, 2012253,];

    public static uint[] HousingAethernet = [MainCities.Limsa_Lominsa_Lower_Decks, MainCities.Uldah_Steps_of_Nald, MainCities.New_Gridania, MainCities.Foundation, MainCities.Kugane];

    public static void SaveGeneratedHousingData()
    {
        EzConfig.SaveConfiguration(P.ResidentialAethernet.HousingData, "GeneratedHousingData.json", false);
    }

    public static float CalculatePathDistance(Vector3[] vectors)
    {
        var distance = 0f;
        for (int i = 0; i < vectors.Length - 1; i++)
        {
            distance += Vector3.Distance(vectors[i], vectors[i + 1]);
        }
        return distance;
    }

    public static bool? WaitForScreen() => IsScreenReady();

    internal static void TryNotify(string s)
    {
        P.NotificationMasterApi.DisplayTrayNotification(P.Name, s);
    }

    internal static string GetDataCenter(string world)
    {
        return Svc.Data.GetExcelSheet<World>().First(x => x.Name == world).DataCenter.Value.Name.ToString();
    }

    internal static int Minutes(this int min)
    {
        return min * 60 * 1000;
    }
    internal static int Seconds(this int sec)
    {
        return sec * 1000;
    }

    internal static bool CanAutoLogin()
    {
        return !Svc.ClientState.IsLoggedIn
            && !Svc.Condition.Any()
            && TryGetAddonByName<AtkUnitBase>("_TitleMenu", out var title)
            && IsAddonReady(title)
            && title->UldManager.NodeListCount > 3
            && title->UldManager.NodeList[3]->Color.A == 0xFF
            && !TryGetAddonByName<AtkUnitBase>("TitleDCWorldMap", out _)
            && !TryGetAddonByName<AtkUnitBase>("TitleConnect", out _);
    }

    internal static Dictionary<WorldChangeAetheryte, uint> WCATerritories = new()
    {
        [WorldChangeAetheryte.Uldah] = 130,
        [WorldChangeAetheryte.Limsa] = 129,
        [WorldChangeAetheryte.Gridania] = 132
    };

    internal static bool TryGetCharacterIndex(string name, uint world, out int index)
    {
        index = GetCharacterNames().IndexOf((name, (ushort)world));
        return index >= 0;
    }

    internal static List<CharaData> GetCharacterNames()
    {
        List<CharaData> ret = [];
        /*var data = CSFramework.Instance()->UIModule->GetRaptureAtkModule()->AtkModule.GetStringArrayData(1);
        if (data != null)
        {
            for (int i = 60; i < data->AtkArrayData.Size; i++)
            {
                if (data->StringArray[i] == null) break;
                var item = data->StringArray[i];
                if (item != null)
                {
                    var str = MemoryHelper.ReadSeStringNullTerminated((nint)item).ExtractText();
                    if (str == "") break;
                    ret.Add(str);
                }
            }
        }*/
        var agent = AgentLobby.Instance();
        if (agent->AgentInterface.IsAgentActive())
        {
            var charaSpan = agent->LobbyData.CharaSelectEntries.Span;
            for (int i = 0; i < charaSpan.Length; i++)
            {
                var s = charaSpan[i];
                ret.Add(($"{MemoryHelper.ReadStringNullTerminated((nint)s.Value->Name)}", s.Value->HomeWorldId));
            }
        }
        return ret;
    }

    internal static GameObject GetReachableWorldChangeAetheryte(bool littleDistance = false) => GetReachableAetheryte(x => Utils.TryGetTinyAetheryteFromGameObject(x, out var ae) && ae?.IsWorldChangeAetheryte() == true, littleDistance);

    internal static GameObject GetReachableResidentialAetheryte(bool littleDistance = false) => GetReachableAetheryte(x => Utils.TryGetTinyAetheryteFromGameObject(x, out var ae) && ae?.IsResidentialAetheryte() == true, littleDistance);

    internal static GameObject GetReachableAetheryte(Predicate<GameObject> predicate, bool littleDistance = false)
    {
        if (!Player.Available) return null;
        var a = Svc.Objects.OrderBy(x => Vector3.DistanceSquared(Player.Object.Position, x.Position)).FirstOrDefault(x => predicate(x));
        if(a != null && a.IsTargetable && Vector3.Distance(a.Position, Player.Object.Position) < (littleDistance?13f:30f))
        {
            return a;
        }
        return null;
    }

    internal static bool IsDisallowedToChangeWorld()
    {
        return Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld]
            || Svc.Condition[ConditionFlag.ReadyingVisitOtherWorld]
            || Svc.Condition[ConditionFlag.InDutyQueue]
            || Svc.Condition[ConditionFlag.BoundByDuty95]
            || Svc.Party.Length > 0
            ;
    }

    internal static bool IsDisallowedToUseAethernet()
    {
        return Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld] || Svc.Condition[ConditionFlag.Jumping];
    }

    internal static string[] Addons =
    [
        "Inventory",
        "InventoryLarge",
        "AreaMap",
        "InventoryRetainerLarge",
        "Currency",
        "Bank",
        "RetainerTask",
        "RetainerList",
        "SelectYesNo",
        "SelectString",
        "SystemMenu",
        "MountNoteBook",
        "FriendList",
        "BlackList",
        "Character",
        "ItemSearch",
        "MonsterNote",
        "GatheringNote",
        "RecipeNote",
        "ContentsFinder",
        "LookingForGroup",
        "Journal",
        "ContentsInfo",
        "SocialList",
        "Macro",
        "ConfigKeybind",
        "GSInfoGeneral",
        "ContentsInfo",
        "PartyMemberList",
        "ContentsFinderConfirm",
        "SelectString",
    ];
    internal static bool IsAddonsVisible(string[] addons)
    {
        foreach(var x in addons)
        {
            if (TryGetAddonByName<AtkUnitBase>(x, out var a) && a->IsVisible) return true;
        }
        return false;
    }
    internal static bool IsMapActive()
    {
        if(TryGetAddonByName<AtkUnitBase>("AreaMap", out var map) && IsAddonReady(map) && map->IsVisible)
        {
             return true;
        }
        return false;
    }

    internal static AetheryteUseState CanUseAetheryte()
    {
        if (P.TaskManager.IsBusy || IsOccupied() || IsDisallowedToUseAethernet()) return AetheryteUseState.None;
        if (P.DataStore.Territories.Contains(P.Territory) && P.ActiveAetheryte != null) return AetheryteUseState.Normal;
        if (P.ResidentialAethernet.IsInResidentialZone() && P.ResidentialAethernet.ActiveAetheryte != null) return AetheryteUseState.Residential;
        return AetheryteUseState.None;
    }

    internal static TinyAetheryte GetMaster()
    {
        return P.ActiveAetheryte.Value.IsAetheryte ? P.ActiveAetheryte.Value : P.DataStore.GetMaster(P.ActiveAetheryte.Value);
    }

    internal static bool IsWorldChangeAetheryte(this TinyAetheryte t)
    {
        return t.ID.EqualsAny<uint>(2, 8, 9);
    }

    internal static bool IsResidentialAetheryte(this TinyAetheryte t)
    {
        return t.ID.EqualsAny<uint>(2, 8, 9, 70, 111);
    }

    static Dictionary<ResidentialAetheryte, uint> TerritoryForResidentialAetheryte = new()
    {
        [ResidentialAetheryte.Uldah] = MainCities.Uldah_Steps_of_Nald,
        [ResidentialAetheryte.Gridania] = MainCities.New_Gridania,
        [ResidentialAetheryte.Limsa] = MainCities.Limsa_Lominsa_Lower_Decks,
        [ResidentialAetheryte.Kugane] = MainCities.Kugane,
        [ResidentialAetheryte.Foundation] = MainCities.Foundation,
    };

    static Dictionary<ResidentialAetheryte, uint> ResidentialTerritoryForResidentialAetheryte = new()
    {
        [ResidentialAetheryte.Uldah] = ResidentalAreas.The_Goblet,
        [ResidentialAetheryte.Gridania] = ResidentalAreas.The_Lavender_Beds,
        [ResidentialAetheryte.Limsa] = ResidentalAreas.Mist,
        [ResidentialAetheryte.Kugane] = ResidentalAreas.Shirogane,
        [ResidentialAetheryte.Foundation] = ResidentalAreas.Empyreum,
    };

    static Dictionary<WorldChangeAetheryte, uint> TerritoryForWorldChangeAetheryte = new()
    {
        [WorldChangeAetheryte.Uldah] = MainCities.Uldah_Steps_of_Nald,
        [WorldChangeAetheryte.Gridania] = MainCities.New_Gridania,
        [WorldChangeAetheryte.Limsa] = MainCities.Limsa_Lominsa_Lower_Decks,
    };

    internal static uint GetTerritory(this ResidentialAetheryte r)
    {
        return TerritoryForResidentialAetheryte[r];
    }
    internal static uint GetResidentialTerritory(this ResidentialAetheryte r)
    {
        return ResidentialTerritoryForResidentialAetheryte[r];
    }

    internal static uint GetTerritory(this WorldChangeAetheryte r)
    {
        return TerritoryForWorldChangeAetheryte[r];
    }

    internal static ResidentialAetheryte? GetResidentialAetheryteByTerritoryType(uint territoryType)
    {
        return TerritoryForResidentialAetheryte.FindKeysByValue(territoryType).FirstOrDefault();
    }

    internal static WorldChangeAetheryte? GetWorldChangeAetheryteByTerritoryType(uint territoryType)
    {
        return TerritoryForWorldChangeAetheryte.FindKeysByValue(territoryType).FirstOrDefault();
    }

    internal static bool TryGetTinyAetheryteFromGameObject(GameObject a, out TinyAetheryte? t, uint? TerritoryType = null)
    {
        TerritoryType ??= Svc.ClientState.TerritoryType;
        if(a == null)
        {
            t = default;
            return false;
        }
        if (a.ObjectKind == ObjectKind.Aetheryte)
        {
            var pos2 = a.Position.ToVector2();
            foreach (var x in P.DataStore.Aetherytes)
            {
                if (x.Key.TerritoryType == TerritoryType && Vector2.Distance(x.Key.Position, pos2) < 10)
                {
                    t = x.Key;
                    return true;
                }
                foreach (var l in x.Value)
                {
                    if (l.TerritoryType == TerritoryType && Vector2.Distance(l.Position, pos2) < 10)
                    {
                        t = l;
                        return true;
                    }
                }
            }
        }
        t = default;
        return false;
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

    internal static AtkUnitBase* GetSpecificYesno(params string[] s) => GetSpecificYesno(false, s);

    internal static AtkUnitBase* GetSpecificYesno(bool contains, params string[] s)
    {
        for (int i = 1; i < 100; i++)
        {
            try
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno", i);
                if (addon == null) return null;
                if (IsAddonReady(addon))
                {
                    var textNode = addon->UldManager.NodeList[15]->GetAsAtkTextNode();
                    var text = MemoryHelper.ReadSeString(&textNode->NodeText).ExtractText().Replace(" ", "");
                    if (contains? 
                        text.ContainsAny(s.Select(x => x.Replace(" ", "")))
                        :text.EqualsAny(s.Select(x => x.Replace(" ", "")))
                        )
                    {
                        PluginLog.Verbose($"SelectYesno {s.Print()} addon {i}");
                        return addon;
                    }
                }
            }
            catch (Exception e)
            {
                e.Log();
                return null;
            }
        }
        return null;
    }

    internal static string[] GetAvailableWorldDestinations()
    {
        if (TryGetAddonByName<AtkUnitBase>("WorldTravelSelect", out var addon) && IsAddonReady(addon))
        {
            List<string> arr = [];
            for (int i = 3; i <= 9; i++)
            {
                var item = addon->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];
                var text = MemoryHelper.ReadSeString(&item->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode()->NodeText).ExtractText();
                if (text == "") break;
                arr.Add(text);
            }
            return [.. arr];
        }
        return Array.Empty<string>();
    }

    internal static string[] GetAvailableAethernetDestinations()
    {
        if (TryGetAddonByName<AtkUnitBase>("TelepotTown", out var addon) && IsAddonReady(addon))
        {
            List<string> arr = [];
            for (int i = 1; i <= 52; i++)
            {
                var item = addon->UldManager.NodeList[16]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];
                var text = MemoryHelper.ReadSeString(&item->GetAsAtkComponentNode()->Component->UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText).ExtractText().Trim();
                if (text == "") break;
                arr.Add(text);
            }
            return [.. arr];
        }
        return Array.Empty<string>();
    }

    internal static GameObject GetValidAetheryte()
    {
        foreach(var x in Svc.Objects)
        {
            if(x.IsAetheryte())
            {
                var d2d = Vector2.Distance(Svc.ClientState.LocalPlayer.Position.ToVector2(), x.Position.ToVector2());
                var d3d = Vector3.Distance(Svc.ClientState.LocalPlayer.Position, x.Position);
                if (P.ResidentialAethernet.IsInResidentialZone() && d3d > 4.6f) continue;

                if (d2d < 11f 
                    && d3d < 15f 
                    && x.IsVPosValid() 
                    && x.IsTargetable)
                {
                    return x;
                }
            }
        }
        return null;
    }

    public static bool IsAetheryte(this GameObject obj)
    {
        if (obj.ObjectKind == ObjectKind.Aetheryte) return true;
        return Utils.AethernetShards.Contains(obj.DataId);
    }

    internal static bool IsVPosValid(this GameObject x)
    {
        /*if(x.Name.ToString() == Lang.AethernetShard)
        {
            return MathF.Abs(Player.Object.Position.Y - x.Position.Y) < 0.965;
        }*/
        return true;
    }

    internal static bool TrySelectSpecificEntry(string text, Func<bool> Throttle)
    {
        return TrySelectSpecificEntry(new string[] {text }, Throttle);  
    }

    internal static bool TrySelectSpecificEntry(IEnumerable<string> text, Func<bool> Throttle)
    {
        if (TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && IsAddonReady(&addon->AtkUnitBase))
        {
            var entry = GetEntries(addon).FirstOrDefault(x => x.EqualsAny(text));
            if (entry != null)
            {
                var index = GetEntries(addon).IndexOf(entry);
                if (index >= 0 && IsSelectItemEnabled(addon, index) && Throttle())
                {
                    ClickSelectString.Using((nint)addon).SelectItem((ushort)index);
                    PluginLog.Debug($"TrySelectSpecificEntry: selecting {entry}/{index} as requested by {text.Print()}");
                    return true;
                }
            }
        }
        return false;
    }

    internal static bool IsSelectItemEnabled(AddonSelectString* addon, int index)
    {
        var step1 = (AtkTextNode*)addon->AtkUnitBase
                    .UldManager.NodeList[2]
                    ->GetComponent()->UldManager.NodeList[index + 1]
                    ->GetComponent()->UldManager.NodeList[3];
        return GenericHelpers.IsSelectItemEnabled(step1);
    }

    internal static List<string> GetEntries(AddonSelectString* addon)
    {
        var list = new List<string>();
        for (int i = 0; i < addon->PopupMenu.PopupMenu.EntryCount; i++)
        {
            list.Add(MemoryHelper.ReadSeStringNullTerminated((nint)addon->PopupMenu.PopupMenu.EntryNames[i]).ExtractText().Trim());
        }
        //PluginLog.Debug($"{list.Print()}");
        return list;
    }

    internal static int GetServiceAccount(string name, uint world) => GetServiceAccount($"{name}@{ExcelWorldHelper.GetName(world)}");

    internal static int GetServiceAccount(string nameWithWorld)
    {
        if (P.AutoRetainerApi?.Ready == true && P.Config.UseAutoRetainerAccounts)
        {
            var chars = P.AutoRetainerApi.GetRegisteredCharacters();
            foreach (var c in chars)
            {
                var data = P.AutoRetainerApi.GetOfflineCharacterData(c);
                if (data != null)
                {
                    var name = $"{data.Name}@{data.World}";
                    if(nameWithWorld == name && data.ServiceAccount > -1)
                    {
                        return data.ServiceAccount;
                    }
                }
            }
        }
        if(P.Config.ServiceAccounts.TryGetValue(nameWithWorld, out var ret))
        {
            if (ret > -1) return ret;
        }
        return 0;
    }
}
