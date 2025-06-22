using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.Automation;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Lifestream.Schedulers;
using Lifestream.Tasks.Utility;

namespace Lifestream.Tasks.SameWorld;
public static class TaskApproachHousingAetheryte
{
    public static readonly (Vector3 Pos, float Distance) EmpyreumIMP = (new Vector3(20.540209f, -15.2f, 179.47063f), 29.78f);
    public static readonly (Vector3 Pos, float Distance) LavenderIMP = (new Vector3(3.1033986f, 2.8884888f, 191.80864f), 9.35f);
    public static readonly (Vector3 Pos, float Distance) ShiroIMP = (new Vector3(-103.11274f, 2.02f, 129.29942f), 8f);
    public static void Enqueue()
    {
        P.TaskManager.EnqueueMulti(
            C.WaitForScreenReady ? new(Utils.WaitForScreen) : null,
            new(() => TaskMoveToHouse.UseSprint(false)),
            new(MoveIMP),
            new(WaitUntilArrivesAtIMP),
            new(TargetNearestShard),
            new(WorldChange.LockOn),
            new(WorldChange.EnableAutomove),
            new(() => S.Data.ResidentialAethernet.ActiveAetheryte != null, "Wait until residential aetheryte exists"),
            new(WorldChange.DisableAutomove)
            );
    }

    public static void MoveIMP()
    {
        if(P.Territory.EqualsAny(ResidentalAreas.Empyreum))
        {
            P.FollowPath.Move([EmpyreumIMP.Pos], true);
        }
        else if(P.Territory.EqualsAny(ResidentalAreas.Shirogane, ResidentalAreas.The_Lavender_Beds))
        {
            Chat.ExecuteCommand("/automove on");
        }
    }

    public static bool WaitUntilArrivesAtIMP()
    {
        if(P.Territory == ResidentalAreas.Empyreum)
        {
            return !P.FollowPath.Waypoints.Any();
        }
        if(P.Territory == ResidentalAreas.The_Lavender_Beds)
        {
            return Svc.Objects.Any(x => Utils.AethernetShards.Contains(x.DataId) && Vector3.Distance(Player.Object.Position, x.Position) < LavenderIMP.Distance);
        }
        if(P.Territory == ResidentalAreas.Shirogane)
        {
            return Player.Object.Position.Z < 128f;
        }
        return true;
    }

    //public static bool WaitUntilMovementStopped() => P.FollowPath.Waypoints.Count == 0;

    public static bool TargetNearestShard()
    {
        if(!Player.Interactable) return false;
        foreach(var x in Svc.Objects.OrderBy(z => Vector3.Distance(Player.Object.Position, z.Position)))
        {
            if(Utils.AethernetShards.Contains(x.DataId) && x.IsTargetable && x.ObjectKind == ObjectKind.EventObj)
            {
                if(EzThrottler.Throttle("TargetNearestShard"))
                {
                    Svc.Targets.SetTarget(x);
                    return true;
                }
            }
        }
        return false;
    }
}
