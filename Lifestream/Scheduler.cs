using ECommons.GameFunctions;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
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
            if(a != null && a.Address != Svc.Targets.Target?.Address)
            {
                if (EzThrottler.Throttle("TargetValidAetheryte", 500))
                {
                    Svc.Targets.SetTarget(a);
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
            return Util.TrySelectSpecificEntry("Visit Another World Server", () => EzThrottler.Throttle("SelectString"));
        }

        internal static bool? SelectVisitAnotherWorld()
        {
            return Util.TrySelectSpecificEntry("Visit Another World Server", () => EzThrottler.Throttle("SelectString"));
        }

        internal static bool? SelectWorldToVisit(string world)
        {
            var worlds = Util.GetAvailableDestinations();
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
    }
}
