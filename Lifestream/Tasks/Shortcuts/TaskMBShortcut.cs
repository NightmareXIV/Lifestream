using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lifestream.Schedulers;
using Lifestream.Tasks.SameWorld;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.Shortcuts;
public unsafe static class TaskMBShortcut
{
    public static void Enqueue()
    {
        if (P.ActiveAetheryte == null || P.ActiveAetheryte.Value.ID != 9)
        {
            TaskReturnToGateway.Enqueue(Enums.WorldChangeAetheryte.Uldah);
        }
        TaskTryTpToAethernetDestination.Enqueue(Svc.Data.GetExcelSheet<Aetheryte>().GetRow(125).AethernetName.Value.Name);
        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
        P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
        P.TaskManager.Enqueue(() =>
        {
            if (!GetMarketBoard().IsTarget())
            {
                if (EzThrottler.Throttle("TargetMB")) Svc.Targets.Target = GetMarketBoard();
                return false;
            }
            else
            {
                if (EzThrottler.Throttle("LockOnMb"))
                {
                    Chat.Instance.ExecuteCommand("/lockon");
                    return true;
                }
            }
            return false;
        });
        P.TaskManager.Enqueue(WorldChange.EnableAutomove);
        P.TaskManager.Enqueue(() => Vector3.Distance(Player.Position, Svc.Targets.Target.Position) < 4f);
        P.TaskManager.Enqueue(WorldChange.DisableAutomove);
        P.TaskManager.Enqueue(() =>
        {
            if (!Player.IsAnimationLocked)
            {
                if (GetMarketBoard().IsTarget())
                {
                    if (EzThrottler.Throttle("InteractWithMB"))
                    {
                        TargetSystem.Instance()->InteractWithObject(Svc.Targets.Target.Struct(), false);
                        return true;
                    }
                }
            }
            return false;
        });
    }

    static IGameObject GetMarketBoard()
    {
        return Svc.Objects.OrderBy(x => Vector3.Distance(Player.Position, x.Position)).FirstOrDefault(x => x.DataId == 2000442);
    }
}
