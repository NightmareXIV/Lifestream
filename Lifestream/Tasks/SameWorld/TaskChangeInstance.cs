using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.Automation.NeoTaskManager;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.SameWorld;
public unsafe static class TaskChangeInstance
{
    public static readonly char[] InstanceNumbers = "\0".ToCharArray();

    public static void Enqueue(int number)
    {
        var tasks = new TaskManagerTask[]
        {
            new(InteractWithAetheryte),
            new(SelectTravel),
            new(() => SelectInstance(number), $"SelectInstance({number})"),
            new(() => !IsOccupied()),
            new(() =>
            {
                if(P.Config.InstanceSwitcherRepeat && number != S.InstanceHandler.GetInstance())
                {
                    Enqueue(number);
                }
            })
        };
        P.TaskManager.EnqueueMulti(tasks);
    }

    public static bool SelectInstance(int num)
    {
        if (TryGetAddonMaster<AddonMaster.SelectString>(out var m) && m.IsAddonReady)
        {
            foreach (var x in m.Entries)
            {
                if (x.Text.Contains(InstanceNumbers[num]))
                {
                    if (EzThrottler.Throttle("SelectTravelToInstance"))
                    {
                        x.Select();
                        return true;
                    }
                    return false;
                }
            }
        }
        return false;
    }

    public static bool SelectTravel()
    {
        if(TryGetAddonMaster<AddonMaster.SelectString>(out var m) && m.IsAddonReady)
        {
            foreach(var x in m.Entries)
            {
                if (x.Text.ContainsAny(Lang.TravelToInstancedArea))
                {
                    if (EzThrottler.Throttle("SelectTravelToInstancedArea"))
                    {
                        x.Select();
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public static bool InteractWithAetheryte()
    {
        if (Svc.Condition[ConditionFlag.OccupiedInQuestEvent]) return true;
        var aetheryte = GetAetheryte() ?? throw new NullReferenceException();
        if (aetheryte.IsTarget())
        {
            if (EzThrottler.Throttle("InteractWithAetheryte"))
            {
                TargetSystem.Instance()->InteractWithObject(aetheryte.Struct());
                return false;
            }
        }
        else
        {
            if (EzThrottler.Throttle("AetheryteSetTarget"))
            {
                Svc.Targets.Target = aetheryte;
                return false;
            }
        }
        return false;
    }

    public static IGameObject GetAetheryte()
    {
        foreach(var x in Svc.Objects)
        {
            if(x.ObjectKind == ObjectKind.Aetheryte && x.IsTargetable)
            {
                if(Vector3.Distance(x.Position, Player.Position) < 11f)
                {
                    return x;
                }
            }
        }
        return null;
    }
}
