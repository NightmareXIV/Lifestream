using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lifestream.Tasks.SameWorld;
using Lumina.Excel.GeneratedSheets;

namespace Lifestream.Tasks.Shortcuts;
public static unsafe class TaskMBShortcut
{
    private static Vector3[] Path = [new(139.2f, 4.0f, -31.8f), new(145.3f, 4.0f, -31.8f)];

    public static void Enqueue()
    {
        if(P.ActiveAetheryte == null || P.ActiveAetheryte.Value.ID != 9)
        {
            TaskReturnToGateway.Enqueue(Enums.WorldChangeAetheryte.Uldah, true);
        }
        TaskTryTpToAethernetDestination.Enqueue(Svc.Data.GetExcelSheet<Aetheryte>().GetRow(125).AethernetName.Value.Name);
        P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
        P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
        P.TaskManager.Enqueue(() =>
        {
            if(Vector3.Distance(Player.Position, Path[0]) < 15f)
            {
                P.FollowPath.Move([.. Path], true);
                return true;
            }
            return false;
        });
        P.TaskManager.Enqueue(() => P.FollowPath.Waypoints.Count == 0);
        P.TaskManager.Enqueue(() =>
        {
            if(!Utils.DismountIfNeeded()) return false;
            if(!GetMarketBoard().IsTarget())
            {
                if(EzThrottler.Throttle("TargetMB")) Svc.Targets.Target = GetMarketBoard();
                return false;
            }
            else if(!Player.IsAnimationLocked)
            {
                var board = GetMarketBoard();
                if(board.IsTargetable)
                {
                    if(EzThrottler.Throttle("InteractWithMB"))
                    {
                        TargetSystem.Instance()->InteractWithObject(board.Struct(), false);
                        return true;
                    }
                }
            }
            return false;
        });
    }

    private static IGameObject GetMarketBoard()
    {
        return Svc.Objects.OrderBy(x => Vector3.Distance(Player.Position, x.Position)).FirstOrDefault(x => x.DataId == 2000442);
    }
}
