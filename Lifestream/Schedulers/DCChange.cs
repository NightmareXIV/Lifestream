using ClickLib.Clicks;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.Automation;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lifestream.Schedulers
{
    internal static unsafe class DCChange
    {
        internal static bool DCThrottle => FrameThrottler.Throttle("DCOperation", 10);
        internal static bool DCRethrottle() => FrameThrottler.Throttle("DCOperation", 10, true);

        internal static bool? WaitUntilNotBusy()
        {
            if (!Player.Available) return false;
            return Player.Object.CastActionId == 0 && !IsOccupied() && Player.Object.IsTargetable();
        }

        internal static bool? Logout()
        {
            if (DCThrottle)
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
            if (Svc.ClientState.IsLoggedIn)
            {
                return true;
            }
            var addon = Util.GetSpecificYesno(true, "Logging in with");
            if (addon == null || !IsAddonReady(addon))
            {
                DCRethrottle();
                return false;
            }
            if (DCThrottle)
            {
                PluginLog.Debug($"[DCChange] Confirming login");
                ClickSelectYesNo.Using((nint)addon).Yes();
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool? SelectYesLogout()
        {
            if (!Svc.ClientState.IsLoggedIn)
            {
                return true;
            }
            var addon = Util.GetSpecificYesno(Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Addon>()?.GetRow(115)?.Text.ToDalamudString().ExtractText());
            if (addon == null || !IsAddonReady(addon))
            {
                DCRethrottle();
                return false;
            }
            if (DCThrottle)
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


        internal static bool? SelectCharacter(string name)
        {
            // Select Character
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("_CharaSelectListMenu", 1);
            if (addon == null) return false;
            if (Util.TryGetCharacterIndex(name, out var index))
            {
                if (DCThrottle)
                {
                    PluginLog.Debug($"[DCChange] Selecting character index {index}");
                    Callback.Fire(addon, false, (int)17, (int)0, (int)index);
                    var nextAddon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno", 1);
                    return nextAddon != null;
                }
            }
            else
            {
                DCRethrottle();
            }
            return false;
        }

        internal static bool? WaitUntilCanAutoLogin()
        {
            return Util.CanAutoLogin();
        }

        internal static bool? TitleScreenClickStart()
        {
            if (!Util.CanAutoLogin())
            {
                DCRethrottle();
                return true;
            }
            if(Util.CanAutoLogin() && TryGetAddonByName<AtkUnitBase>("_TitleMenu", out var title) && IsAddonReady(title) && DCThrottle && EzThrottler.Throttle("TitleScreenClickStart"))
            {
                PluginLog.Debug($"[DCChange] Clicking start");
                Callback.Fire(title, true, (int)1);
                DCRethrottle();
                return false;
            }
            else
            {
                DCRethrottle();
            }
            return false;
        }

        internal static bool? OpenContextMenuForChara(string name)
        {
            if(TryGetAddonByName<AddonContextMenu>("ContextMenu", out var m) && IsAddonReady(&m->AtkUnitBase))
            {
                DCRethrottle();
                return true;
            }
            if (TryGetAddonByName<AtkUnitBase>("_CharaSelectListMenu", out var addon) && IsAddonReady(addon))
            {
                if (Util.TryGetCharacterIndex(name, out var index) && DCThrottle && EzThrottler.Throttle("OpenContextMenuForChara"))
                {
                    PluginLog.Debug($"[DCChange] Opening context menu index {index}");
                    Callback.Fire(addon, true, (int)17, (int)1, (int)index);
                    DCRethrottle();
                    return false;
                }
            }
            else
            {
                DCRethrottle();
            }
            return false;
        }

        internal static bool? SelectVisitAnotherDC()
        {
            if (TryGetAddonByName<AddonContextMenu>("ContextMenu", out var menu) && IsAddonReady(&menu->AtkUnitBase))
            {
                var addon = menu->AtkUnitBase;
                var list = addon.UldManager.NodeList[2];
                var item = list->GetAsAtkComponentNode()->Component->UldManager.NodeList[9];
                var textNode = item->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];
                if (textNode->Alpha_2 == 255)
                {
                    var text = MemoryHelper.ReadSeString(&textNode->GetAsAtkTextNode()->NodeText).ExtractText();
                    if (text.EqualsAny("Visit Another Data Center") && DCThrottle && EzThrottler.Throttle("SelectVisitAnotherDC"))
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
            if (TryGetAddonByName<AddonContextMenu>("ContextMenu", out var menu) && IsAddonReady(&menu->AtkUnitBase))
            {
                var addon = menu->AtkUnitBase;
                var list = addon.UldManager.NodeList[2];
                var item = list->GetAsAtkComponentNode()->Component->UldManager.NodeList[7];
                var textNode = item->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];
                if (textNode->Alpha_2 == 255)
                {
                    var text = MemoryHelper.ReadSeString(&textNode->GetAsAtkTextNode()->NodeText).ExtractText();
                    if (text.EqualsAny("Return to Home World") && DCThrottle && EzThrottler.Throttle("SelectReturnToHomeWorld"))
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
            if (TryGetAddonByName<AtkUnitBase>("LobbyDKTCheck", out var addon) && IsAddonReady(addon) && addon->UldManager.NodeList[3]->GetAsAtkComponentButton()->IsEnabled)
            {
                if (DCThrottle)
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
            if (TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
            {
                var cw = MemoryHelper.ReadSeString(&addon->UldManager.NodeList[13]->GetAsAtkTextNode()->NodeText).ExtractText();
                if(cw == name)
                {
                    return true;
                }
                var list = addon->UldManager.NodeList[7]->GetAsAtkComponentNode();
                var num = 0;
                for (int i = 3; i < 3+4; i++)
                {
                    var t = list->Component->UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList[8]->GetAsAtkTextNode();
                    if (t->AtkResNode.Alpha_2 == 255)
                    {
                        var text = MemoryHelper.ReadSeString(&t->NodeText).ExtractText();
                        if (text != "") num++;
                        if (text == name && DCThrottle && EzThrottler.Throttle("SelectTargetDataCenter"))
                        {
                            PluginLog.Debug($"[DCChange] Selecting Target DC {name} index {i}");
                            P.Memory.ConstructEvent(addon, 1, 7, i - 2);
                            DCRethrottle();
                            return false;
                        }
                    }
                }
                if (num > 0) DCRethrottle();
            }
            else
            {
                DCRethrottle();
            }
            return false;
        }

        internal static bool? SelectTargetWorld(string name)
        {
            if (TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
            {
                var cw = MemoryHelper.ReadSeString(&addon->UldManager.NodeList[10]->GetAsAtkTextNode()->NodeText).ExtractText();
                if (cw == name)
                {
                    return true;
                }
                var list = addon->UldManager.NodeList[6]->GetAsAtkComponentNode();
                var num = 0;
                for (int i = 3; i < 3+8; i++)
                {
                    var t = list->Component->UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList[8]->GetAsAtkTextNode();
                    if (t->AtkResNode.Alpha_2 == 255)
                    {
                        var text = MemoryHelper.ReadSeString(&t->NodeText).ExtractText();
                        if (text != "") num++;
                        if (text == name && DCThrottle && EzThrottler.Throttle("SelectTargetWorld"))
                        {
                            PluginLog.Debug($"[DCChange] Selecting target world {name} index {i}");
                            P.Memory.ConstructEvent(addon, 2, 6, i - 2);
                            DCRethrottle();
                            return false;
                        }
                    }
                }
                if (num == 0) DCRethrottle();
            }
            else
            {
                DCRethrottle();
            }
            return false;
        }

        internal static bool? ConfirmDcVisit()
        {
            if (TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
            {
                if (DCThrottle && addon->UldManager.NodeList[5]->GetAsAtkComponentButton()->IsEnabled && EzThrottler.Throttle("ConfirmDcVisit", 5000))
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
            return false;
        }

        internal static bool? ConfirmDcVisit2()
        {
            if (TryGetAddonByName<AtkUnitBase>("LobbyDKTCheckExec", out var addon) && IsAddonReady(addon))
            {
                if (DCThrottle && addon->UldManager.NodeList[3]->GetAsAtkComponentButton()->IsEnabled && EzThrottler.Throttle("ConfirmDcVisit", 5000))
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
            return false;
        }

        internal static bool? SelectOk()
        {
            if (TryGetAddonByName<AtkUnitBase>("SelectOk", out var addon) && IsAddonReady(addon))
            {
                if (DCThrottle && EzThrottler.Throttle("SelectOk", 500))
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
    }
}
