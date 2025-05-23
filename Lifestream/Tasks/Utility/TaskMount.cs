﻿using ECommons.Automation;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.Utility;
public unsafe static class TaskMount
{
    public static bool MountIfCan()
    {
        if(Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        if(C.Mount == -1) return true;
        if(Svc.Condition[ConditionFlag.MountOrOrnamentTransition] || Svc.Condition[ConditionFlag.Casting])
        {
            EzThrottler.Throttle("CheckMount", 2000, true);
        }
        if(!EzThrottler.Check("CheckMount")) return false;
        if(ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, 9) == 0)
        {
            var mount = C.Mount;
            if(mount == 0 || !PlayerState.Instance()->IsMountUnlocked((uint)mount))
            {
                var mounts = Svc.Data.GetExcelSheet<Mount>().Where(x => x.Singular != "" && PlayerState.Instance()->IsMountUnlocked(x.RowId));
                if(mounts.Any())
                {
                    var newMount = (int)mounts.GetRandom().RowId;
                    PluginLog.Warning($"Mount {Utils.GetMountName(mount)} is not unlocked. Randomly selecting {Utils.GetMountName(newMount)}.");
                    mount = newMount;
                }
                else
                {
                    PluginLog.Warning("No unlocked mounts found");
                    return true;
                }
            }
            if(!Player.IsAnimationLocked && EzThrottler.Throttle("SummonMount"))
            {
                Chat.ExecuteCommand($"/mount \"{Utils.GetMountName(mount)}\"");
            }
        }
        else
        {
            return true;
        }
        return false;
    }
}
