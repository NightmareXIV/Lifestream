using ClickLib.Clicks;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.StringHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Havok;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal unsafe static class Scheduler
    {        
        internal static bool? TargetValidAetheryte()
        {
            if (IsOccupied()) return false;
            var a = Util.GetValidAetheryte();
            if(a != null)
            {
                if (a.Address != Svc.Targets.Target?.Address)
                {
                    if (EzThrottler.Throttle("TargetValidAetheryte", 500))
                    {
                        Svc.Targets.SetTarget(a);
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool? InteractWithTargetedAetheryte()
        {
            if (IsOccupied()) return false;
            var a = Util.GetValidAetheryte();
            if(a != null && Svc.Targets.Target?.Address == a.Address)
            {
                if(EzThrottler.Throttle("InteractWithTargetedAetheryte", 500))
                {
                    TargetSystem.Instance()->InteractWithObject(a.Struct(), false);
                    return true;
                }
            }
            return false;
        }

        internal static bool? SelectAethernet()
        {
            return Util.TrySelectSpecificEntry(Lang.Aethernet, () => EzThrottler.Throttle("SelectString"));
        }

        internal static bool? SelectVisitAnotherWorld()
        {
            return Util.TrySelectSpecificEntry(Lang.VisitAnotherWorld, () => EzThrottler.Throttle("SelectString"));
        }

        internal static bool? ConfirmWorldVisit(string s)
        {
            var x = (AddonSelectYesno*)Util.GetSpecificYesno(true, $"Travel to", "へ移動します、よろしいですか？", "Nach reisen?", "Voulez-vous vraiment visiter");
            if (x != null)
            {
                if (x->YesButton->IsEnabled && EzThrottler.Throttle("ConfirmWorldVisit"))
                {
                    ClickSelectYesNo.Using((nint)x).Yes();
                    return true;
                }
            }
            return false;
        }

        internal static bool? SelectWorldToVisit(string world)
        {
            var worlds = Util.GetAvailableWorldDestinations();
            var index = Array.IndexOf(worlds, world);
            if (index != -1)
            {
                if (TryGetAddonByName<AtkUnitBase>("WorldTravelSelect", out var addon) && IsAddonReady(addon))
                {
                    if (EzThrottler.Throttle("SelectWorldToVisit", 5000))
                    {
                        Callback(addon, index + 2);
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool? TeleportToAethernetDestination(TinyAetheryte t)
        {
            if (TryGetAddonByName<AtkUnitBase>("TelepotTown", out var telep) && IsAddonReady(telep))
            {
                if (P.DataStore.StaticData.Callback.TryGetValue(t.ID, out var callback))
                {
                    if (Util.GetAvailableAethernetDestinations().Any(x => x.ESEquals(t.Name)))
                    {
                        if (EzThrottler.Throttle("TeleportToAethernetDestination", 2000))
                        {
                            P.TaskManager.EnqueueImmediate(() => Callback(telep, (int)11, (uint)callback));
                            P.TaskManager.EnqueueImmediate(() => Callback(telep, (int)11, (uint)callback));
                            return true;
                        }
                    }
                    else
                    {
                        if(EzThrottler.Throttle("TeleportToAethernetDestinationLog", 5000))
                        {
                            PluginLog.Warning($"GetAvailableAethernetDestinations does not contains {t.Name}, contains {Util.GetAvailableAethernetDestinations().Print()}");
                        }
                    }
                }
                else
                {
                    DuoLog.Error($"Callback data absent for {t.Name}");
                    return null;
                }
            }
            return false;
        }

        internal static bool? ExecuteTPCommand()
        {
            if (AgentMap.Instance()->IsPlayerMoving == 0 && !IsOccupied() && !Player.Object.IsCasting && EzThrottler.Throttle("ExecTP"))
            {
                Svc.Commands.ProcessCommand("/tp Ul'dah - Steps of Nald");
                return true;
            }
            return false;
        }

        internal static bool? WaitUntilNextToAetheryteAndNotBusy()
        {
            return P.ActiveAetheryte != null && P.DataStore.Territories.Contains(P.Territory) && P.ActiveAetheryte != null && !IsOccupied() && !Util.IsDisallowedToUseAethernet() && P.ActiveAetheryte.Value.IsWorldChangeAetheryte() && Player.Object.IsTargetable();
        }
    }
}
