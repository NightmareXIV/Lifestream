
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.Automation;
using ECommons.Automation.UIInput;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.AtkReaders;
using Lifestream.Tasks.Login;
using Lumina.Excel.GeneratedSheets;

namespace Lifestream.Schedulers;

internal static unsafe class DCChange
{
    internal static bool DCThrottle => FrameThrottler.Throttle("DCOperation", 10);
    internal static bool DCRethrottle() => FrameThrottler.Throttle("DCOperation", 10, true);

    internal static bool? WaitUntilNotBusy()
    {
        if(!Player.Available) return false;
        return Player.Object.CastActionId == 0 && !IsOccupied() && Player.Object.IsTargetable;
    }

    internal static bool? Logout()
    {
        if(DCThrottle)
        {
            DCRethrottle();
            PluginLog.Debug($"[DCChange] Sending logout command");
            Chat.Instance.SendMessage("/logout");
            return true;
        }
        return false;
    }

    internal static bool? SelectYesLogin()
    {
        if(Svc.ClientState.IsLoggedIn)
        {
            return true;
        }
        {
            if(TryGetAddonByName<AtkUnitBase>("SelectOk", out var addon) && IsAddonReady(addon))
            {
                return true;
            }
        }
        {
            var addon = Utils.GetSpecificYesno(true, Lang.LogInPartialText);
            if(addon == null || !IsAddonReady(addon))
            {
                DCRethrottle();
                return false;
            }
            if(DCThrottle)
            {
                PluginLog.Debug($"[DCChange] Confirming login");
                new AddonMaster.SelectYesno(addon).Yes();
                return false;
            }
            else
            {
                return false;
            }
        }
    }

    internal static bool? SelectYesLogout()
    {
        if(!Svc.ClientState.IsLoggedIn)
        {
            return true;
        }
        var addon = Utils.GetSpecificYesno(Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Addon>()?.GetRow(115)?.Text.ToDalamudString().ExtractText());
        if(addon == null || !IsAddonReady(addon))
        {
            DCRethrottle();
            return false;
        }
        if(DCThrottle)
        {
            PluginLog.Debug($"[DCChange] Confirming logout");
            Callback.Fire(addon, true, 0);
            return true;
        }
        else
        {
            return false;
        }
    }


    internal static bool? SelectCharacter(string name, uint world)
    {
        {
            // Select Character
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("_CharaSelectListMenu", 1);
            PluginLog.Debug($"Select1");
            if(addon == null) return false;
            PluginLog.Debug($"Select1-1");
            //if (!AgentLobby.Instance()->AgentInterface.IsAgentActive()) return false;
            PluginLog.Debug($"Select2");
            if(AgentLobby.Instance()->TemporaryLocked) return false;
            PluginLog.Debug($"Select3");
            if(Utils.TryGetCharacterIndex(name, world, out var index))
            {
                PluginLog.Debug($"Select4/{index}");
                if(DCThrottle && EzThrottler.Check("CharaSelectListMenuError"))
                {
                    PluginLog.Debug($"[DCChange] Selecting character index {index}");
                    Callback.Fire(addon, false, (int)29, (int)0, (int)index);
                }
                var nextAddon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno", 1);
                return nextAddon != null;
            }
            else
            {
                DCRethrottle();
            }
        }
        return false;
    }

    internal static bool? WaitUntilCanAutoLogin()
    {
        return Utils.CanAutoLogin();
    }

    internal static bool? TitleScreenClickStart()
    {
        if(!Utils.CanAutoLogin())
        {
            DCRethrottle();
            return true;
        }
        if(Utils.CanAutoLogin() && TryGetAddonByName<AtkUnitBase>("_TitleMenu", out var title) && IsAddonReady(title) && DCThrottle && EzThrottler.Throttle("TitleScreenClickStart"))
        {
            PluginLog.Debug($"[DCChange] Clicking start");
            Callback.Fire(title, true, (int)4);
            DCRethrottle();
            return false;
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? OpenContextMenuForChara(string name, uint world)
    {
        if(TryGetAddonByName<AddonContextMenu>("ContextMenu", out var m) && IsAddonReady(&m->AtkUnitBase))
        {
            DCRethrottle();
            return true;
        }
        if(TryGetAddonByName<AtkUnitBase>("_CharaSelectListMenu", out var addon) && IsAddonReady(addon))
        {
            TaskChangeCharacter.SelectCharacter(name, ExcelWorldHelper.GetName(world), true);
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? SelectVisitAnotherDC()
    {
        if(TryGetAddonByName<AddonContextMenu>("ContextMenu", out var menu) && IsAddonReady(&menu->AtkUnitBase))
        {
            var addon = menu->AtkUnitBase;
            var list = addon.UldManager.NodeList[2];
            var item = list->GetAsAtkComponentNode()->Component->UldManager.NodeList[9];
            var textNode = item->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];
            if(textNode->Alpha_2 == 255)
            {
                var text = MemoryHelper.ReadSeString(&textNode->GetAsAtkTextNode()->NodeText).ExtractText();
                if(text.EqualsAny(Svc.Data.GetExcelSheet<Lobby>().GetRow(1150).Text.ToDalamudString().ExtractText()) && DCThrottle && EzThrottler.Throttle("SelectVisitAnotherDC"))
                {
                    PluginLog.Debug($"[DCChange] Selecting visit another data center");
                    Callback.Fire(&menu->AtkUnitBase, true, (int)0, (int)8, (int)0, new AtkValue() { Type = 0, Int = 0 }, new AtkValue() { Type = 0, Int = 0 });
                    return true;
                }
            }
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? SelectReturnToHomeWorld()
    {
        if(TryGetAddonByName<AddonContextMenu>("ContextMenu", out var menu) && IsAddonReady(&menu->AtkUnitBase))
        {
            var addon = menu->AtkUnitBase;
            var list = addon.UldManager.NodeList[2];
            var item = list->GetAsAtkComponentNode()->Component->UldManager.NodeList[7];
            var textNode = item->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];
            if(textNode->Alpha_2 == 255)
            {
                var text = MemoryHelper.ReadSeString(&textNode->GetAsAtkTextNode()->NodeText).ExtractText();
                //"Return to Home World"
                if(text.EqualsAny(Svc.Data.GetExcelSheet<Lobby>().GetRow(1117).Text.ToDalamudString().ExtractText()) && DCThrottle && EzThrottler.Throttle("SelectReturnToHomeWorld"))
                {
                    PluginLog.Debug($"[DCChange] Selecting return to home world");
                    Callback.Fire(&menu->AtkUnitBase, true, (int)0, (int)6, (int)0, new AtkValue() { Type = 0, Int = 0 }, new AtkValue() { Type = 0, Int = 0 });
                    return true;
                }
            }
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? ConfirmDcVisitIntention()
    {
        if(TryGetAddonByName<AtkUnitBase>("LobbyDKTCheck", out var addon) && IsAddonReady(addon) && addon->UldManager.NodeList[3]->GetAsAtkComponentButton()->IsEnabled)
        {
            if(DCThrottle)
            {
                PluginLog.Debug($"[DCChange] Confirming DC visit intention");
                Callback.Fire(addon, true, 0);
                return true;
            }
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? SelectTargetDataCenter(string name)
    {
        if(TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
        {
            var reader = new ReaderLobbyDKTWorldList(addon);
            var cw = MemoryHelper.ReadSeString(&addon->UldManager.NodeList[13]->GetAsAtkTextNode()->NodeText).ExtractText();
            if(reader.SelectedDataCenter == name)
            {
                PluginLog.Information($"SelectTargetDataCenter complete");
                return true;
            }
            var list = addon->UldManager.SearchNodeById(21)->GetAsAtkComponentNode();
            var addonItem = 0;
            var listIndex = 3;
            var category = 0;
            var categoryIndex = 0;
            foreach(var region in reader.Regions)
            {
                addonItem++;
                categoryIndex = 1;
                foreach(var dc in region.DataCenters)
                {
                    if(dc.Name == name)
                    {
                        var t = list->Component->UldManager.NodeList[listIndex]->GetAsAtkComponentNode()->Component->UldManager.NodeList[8]->GetAsAtkTextNode();
                        if(t->AtkResNode.Alpha_2 == 255)
                        {
                            var text = MemoryHelper.ReadSeString(&t->NodeText).ExtractText();
                            if(text == name && DCThrottle && EzThrottler.Throttle("SelectTargetDataCenter"))
                            {
                                PluginLog.Debug($"[DCChange] Selecting Target DC {name} index {addonItem} list {listIndex}");
                                P.Memory.ConstructEvent(addon, category, 1, 7, categoryIndex, addonItem);
                                DCRethrottle();
                                return false;
                            }
                        }
                    }
                    addonItem++;
                    listIndex++;
                    categoryIndex++;
                }
                category++;
            }
            if(reader.Regions.Count == 0) DCRethrottle();
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? SelectTargetWorld(string name, Func<bool> noAvailableWorldsAction)
    {
        if(TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
        {
            var cw = MemoryHelper.ReadSeString(&addon->UldManager.NodeList[10]->GetAsAtkTextNode()->NodeText).ExtractText();
            if(cw == name || (P.Config.DcvUseAlternativeWorld && cw.EqualsAny(ExcelWorldHelper.GetPublicWorlds(Utils.GetDataCenter(name).RowId).Select(w => w.Name.ToString()))))
            {
                return true;
            }
            var list = addon->UldManager.NodeList[6]->GetAsAtkComponentNode();
            var num = 0;
            for(var i = 3; i < 3 + 8; i++)
            {
                var t = list->Component->UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList[8]->GetAsAtkTextNode();
                if(t->AtkResNode.Alpha_2 == 255)
                {
                    var text = MemoryHelper.ReadSeString(&t->NodeText).ExtractText();
                    if(text != "") num++;
                    if(text == name && DCThrottle && EzThrottler.Throttle("SelectTargetWorld"))
                    {
                        PluginLog.Debug($"[DCChange] Selecting target world {name} index {i}");
                        P.Memory.ConstructEvent(addon, 0, 2, 6, i - 2, i - 2);
                        DCRethrottle();
                        return false;
                    }
                }
            }
            if(P.Config.DcvUseAlternativeWorld)
            {
                for(var i = 3; i < 3 + 8; i++)
                {
                    var t = list->Component->UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList[8]->GetAsAtkTextNode();
                    if(t->AtkResNode.Alpha_2 == 255)
                    {
                        var text = MemoryHelper.ReadSeString(&t->NodeText).ExtractText();
                        if(text != "") num++;
                        if(text.EqualsAny(ExcelWorldHelper.GetPublicWorlds(Utils.GetDataCenter(name).RowId).Select(w => w.Name.ToString())) && DCThrottle && EzThrottler.Throttle("SelectTargetWorld"))
                        {
                            PluginLog.Debug($"[DCChange] Selecting alternative target world {name} index {i}");
                            P.Memory.ConstructEvent(addon, 0, 2, 6, i - 2, i - 2);
                            DCRethrottle();
                            return false;
                        }
                    }
                }
            }
            if(num == 0)
            {
                DCRethrottle();
            }
            if(noAvailableWorldsAction != null && TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon2) && IsAddonReady(addon2) && addon2->UldManager.NodeList[4]->GetAsAtkComponentButton()->IsEnabled)
            {
                var result = noAvailableWorldsAction();
                if(result) return true;
            }
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? CancelDcVisit()
    {
        if(TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
        {
            if(addon->UldManager.NodeList[4]->GetAsAtkComponentButton()->IsEnabled)
            {
                if(DCThrottle && EzThrottler.Throttle("CancelDcVisit", 5000))
                {
                    PluginLog.Debug($"[DCChange] Confirming DC visit");
                    addon->UldManager.NodeList[4]->GetAsAtkComponentButton()->ClickAddonButton(addon);
                    return true;
                }
            }
            else
            {
                DCRethrottle();
            }
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? ConfirmDcVisit()
    {
        if(TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
        {
            if(addon->UldManager.NodeList[5]->GetAsAtkComponentButton()->IsEnabled)
            {
                if(DCThrottle && EzThrottler.Throttle("ConfirmDcVisit", 5000))
                {
                    PluginLog.Debug($"[DCChange] Confirming DC visit");
                    Callback.Fire(addon, true, (int)4);
                    return true;
                }
            }
            else
            {
                DCRethrottle();
            }
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? ConfirmDcVisit2()
    {
        if(TryGetAddonByName<AtkUnitBase>("LobbyDKTCheckExec", out var addon) && IsAddonReady(addon))
        {
            if(addon->UldManager.NodeList[3]->GetAsAtkComponentButton()->IsEnabled)
            {
                if(DCThrottle && EzThrottler.Throttle("ConfirmDcVisit", 5000))
                {
                    PluginLog.Debug($"[DCChange] Confirming DC visit 2");
                    Callback.Fire(addon, true, (int)0);
                    return true;
                }
            }
            else
            {
                DCRethrottle();
            }
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? SelectOk()
    {
        if(TryGetAddonByName<AtkUnitBase>("SelectOk", out var addon) && IsAddonReady(addon))
        {
            if(DCThrottle && EzThrottler.Throttle("SelectOk", 500))
            {
                PluginLog.Debug($"[DCChange] Selecting OK");
                Callback.Fire(addon, true, (int)0);
                return true;
            }
        }
        else
        {
            DCRethrottle();
        }
        return false;
    }

    internal static bool? SelectServiceAccount(int account)
    {
        var dcMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("TitleDCWorldMap", 1);
        if(dcMenu != null) dcMenu->Close(true);
        if(TryGetAddonByName<AtkUnitBase>("_CharaSelectWorldServer", out _))
        {
            return true;
        }
        if(TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && IsAddonReady(&addon->AtkUnitBase)
            && addon->AtkUnitBase.UldManager.NodeListCount >= 4)
        {
            var text = MemoryHelper.ReadSeString(&addon->AtkUnitBase.UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText).ExtractText();
            var compareTo = Svc.Data.GetExcelSheet<Lobby>()?.GetRow(11)?.Text.ToString();
            if(text == compareTo)
            {
                PluginLog.Information($"Selecting service account");
                new AddonMaster.SelectString(addon).Entries[account].Select();
                return true;
            }
            else
            {
                PluginLog.Information($"Found different SelectString: {text}");
                return false;
            }
        }
        return false;
    }
}
