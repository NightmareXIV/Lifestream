using ClickLib.Clicks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Enums;
using Lifestream.Tasks;
using Lumina.Excel.GeneratedSheets;
using System.CodeDom;
using CharaData = (string Name, ushort World);

namespace Lifestream;

internal static unsafe class Util
{
    internal static uint GetTerritoryType(this WorldChangeAetheryte wca)
    {
        if (wca == WorldChangeAetheryte.Gridania) return 132;
        if (wca == WorldChangeAetheryte.Uldah) return 130;
        if (wca == WorldChangeAetheryte.Limsa) return 129;
        throw new ArgumentOutOfRangeException(nameof(wca));
    }

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

    internal static GameObject GetReachableWorldChangeAetheryte(bool littleDistance = false)
    {
        if (!Player.Available) return null;
        var a = Svc.Objects.OrderBy(x => Vector3.DistanceSquared(Player.Object.Position, x.Position)).FirstOrDefault(x => Util.TryGetTinyAetheryteFromGameObject(x, out var ae) && ae?.IsWorldChangeAetheryte() == true);
        if(a != null && a.IsTargetable() && Vector3.Distance(a.Position, Player.Object.Position) < (littleDistance?13f:30f))
        {
            return a;
        }
        return null;
    }

    internal static bool IsDisallowedToChangeWorld()
    {
        return Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld]
            || Svc.Condition[ConditionFlag.ReadyingVisitOtherWorld]
            || Svc.Condition[ConditionFlag.BoundToDuty97]
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

    internal static bool CanUseAetheryte()
    {
        return P.DataStore.Territories.Contains(P.Territory) && P.ActiveAetheryte != null && !P.TaskManager.IsBusy && !IsOccupied() && !IsDisallowedToUseAethernet();
    }

    internal static TinyAetheryte GetMaster()
    {
        return P.ActiveAetheryte.Value.IsAetheryte ? P.ActiveAetheryte.Value : P.DataStore.GetMaster(P.ActiveAetheryte.Value);
    }
    
    internal static bool IsWorldChangeAetheryte(this TinyAetheryte t)
    {
        return t.ID.EqualsAny<uint>(2, 8, 9);
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
            List<string> arr = new();
            for (int i = 3; i <= 9; i++)
            {
                var item = addon->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];
                var text = MemoryHelper.ReadSeString(&item->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode()->NodeText).ExtractText();
                if (text == "") break;
                arr.Add(text);
            }
            return arr.ToArray();
        }
        return Array.Empty<string>();
    }

    internal static string[] GetAvailableAethernetDestinations()
    {
        if (TryGetAddonByName<AtkUnitBase>("TelepotTown", out var addon) && IsAddonReady(addon))
        {
            List<string> arr = new();
            for (int i = 1; i <= 52; i++)
            {
                var item = addon->UldManager.NodeList[16]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];
                var text = MemoryHelper.ReadSeString(&item->GetAsAtkComponentNode()->Component->UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText).ExtractText().Trim();
                if (text == "") break;
                arr.Add(text);
            }
            return arr.ToArray();
        }
        return Array.Empty<string>();
    }

    internal static GameObject GetValidAetheryte()
    {
        foreach(var x in Svc.Objects)
        {
            if(x.ObjectKind == ObjectKind.Aetheryte)
            {
                if(Vector2.Distance(Svc.ClientState.LocalPlayer.Position.ToVector2(), x.Position.ToVector2()) < 11f && Vector3.Distance(Svc.ClientState.LocalPlayer.Position, x.Position) < 15f && x.IsVPosValid() && x.IsTargetable())
                {
                    return x;
                }
            }
        }
        return null;
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

    internal static int GetServiceAccount(string name, uint world) => GetServiceAccount($"{name}@{ExcelWorldHelper.GetWorldNameById(world)}");

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
