using ClickLib.Clicks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Memory;
using ECommons;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal static unsafe class Util
    {
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

        internal static string[] Addons = new string[]
        {
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
        };
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
                    if(Vector2.Distance(Svc.ClientState.LocalPlayer.Position.ToVector2(), x.Position.ToVector2()) < 11f && Vector3.Distance(Svc.ClientState.LocalPlayer.Position, x.Position) < 15f && x.IsVPosValid())
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
    }
}
