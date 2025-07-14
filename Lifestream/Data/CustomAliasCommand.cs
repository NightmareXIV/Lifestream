using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Utility;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace Lifestream.Data;
[Serializable]
public class CustomAliasCommand
{
    private static readonly CustomAliasCommand Default = new();

    internal string ID = Guid.NewGuid().ToString();
    public CustomAliasKind Kind;
    public Vector3 Point;
    public uint Aetheryte;
    public int World;
    public Vector2 CenterPoint;
    public Vector3 CircularExitPoint;
    public (float Min, float Max)? Clamp = null;
    public float Precision = 20f;
    public int Tolerance = 1;
    public bool WalkToExit = true;
    public float SkipTeleport = 15f;
    public uint DataID = 0;
    public bool UseTA = false;
    public List<string> SelectOption = [];
    public bool StopOnScreenFade = false;
    public bool NoDisableYesAlready = false;
    public bool UseFlight = false;
    public float Scatter = 0f;
    public bool MountUpConditional = false;

    public bool ShouldSerializeScatter() => Kind.EqualsAny(CustomAliasKind.Move_to_point) && Scatter > 0f;
    public bool ShouldSerializeUseFlight() => Kind.EqualsAny(CustomAliasKind.Move_to_point, CustomAliasKind.Navmesh_to_point) && UseFlight != Default.UseFlight;
    public bool ShouldSerializePoint() => Point != Default.Point;
    public bool ShouldSerializeAetheryte() => Aetheryte != Default.Aetheryte;
    public bool ShouldSerializeWorld() => World != Default.World;
    public bool ShouldSerializeCenterPoint() => CenterPoint != Default.CenterPoint;
    public bool ShouldSerializeCircularExitPoint() => CircularExitPoint != Default.CircularExitPoint;
    public bool ShouldSerializeClamp() => Clamp != Default.Clamp;
    public bool ShouldSerializePrecision() => Precision != Default.Precision;
    public bool ShouldSerializeTolerance() => Tolerance != Default.Tolerance;
    public bool ShouldSerializeWalkToExit() => WalkToExit != Default.WalkToExit;
    public bool ShouldSerializeSkipTeleport() => SkipTeleport != Default.SkipTeleport;
    public bool ShouldSerializeDataID() => DataID != Default.DataID;
    public bool ShouldSerializeUseTA() => UseTA != Default.UseTA;
    public bool ShouldSerializeSelectOption() => SelectOption.Count > 0;
    public bool ShouldSerializeStopOnScreenFade() => StopOnScreenFade != Default.StopOnScreenFade;
    public bool ShouldSerializeNoDisableYesAlready() => NoDisableYesAlready != Default.NoDisableYesAlready;
    public bool ShouldSerializeMountUpConditional() => MountUpConditional != Default.MountUpConditional;

    public void Enqueue(List<Vector3> appendMovement)
    {
        if(Kind == CustomAliasKind.Change_world)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
            if(World != Player.Object.CurrentWorld.RowId)
            {
                var world = ExcelWorldHelper.GetName(World);
                if(S.Ipc.IPCProvider.CanVisitCrossDC(world))
                {
                    P.TPAndChangeWorld(world, true, skipChecks: true);
                }
                else if(S.Ipc.IPCProvider.CanVisitSameDC(world))
                {
                    P.TPAndChangeWorld(world, false, skipChecks: true);
                }
            }
        }
        else if(Kind == CustomAliasKind.Move_to_point)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
            if(UseFlight) P.TaskManager.Enqueue(FlightTasks.FlyIfCan);
            P.TaskManager.Enqueue(() => TaskMoveToHouse.UseSprint(false));
            P.TaskManager.Enqueue(() => P.FollowPath.Move([Point.Scatter(Scatter), .. appendMovement], true));
            P.TaskManager.Enqueue(() => P.FollowPath.Waypoints.Count == 0);
        }
        else if(Kind == CustomAliasKind.Navmesh_to_point)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable && S.Ipc.VnavmeshIPC.IsReady() == true);
            if(UseTA && Svc.PluginInterface.InstalledPlugins.Any(x => x.Name == "TextAdvance" && x.IsLoaded))
            {
                P.TaskManager.Enqueue(() =>
                {
                    S.Ipc.TextAdvanceIPC.EnqueueMoveTo2DPoint(new()
                    {
                        Position = Point,
                        NoInteract = true,
                    }, 5f);
                });
                P.TaskManager.Enqueue(S.Ipc.TextAdvanceIPC.IsBusy, new(abortOnTimeout: false, timeLimitMS: 5000));
                P.TaskManager.Enqueue(() => !S.Ipc.TextAdvanceIPC.IsBusy(), new(timeLimitMS: 1000 * 60 * 5));
                P.TaskManager.Enqueue(() => P.FollowPath.Move([.. appendMovement], true));
                P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
                P.TaskManager.Enqueue(() => P.FollowPath.Waypoints.Count == 0);
            }
            else
            {
                if(UseFlight) P.TaskManager.Enqueue(FlightTasks.FlyIfCan);
                P.TaskManager.Enqueue(() =>
                {
                    var task = S.Ipc.VnavmeshIPC.Pathfind(Player.Position, Point, UseFlight);
                    P.TaskManager.InsertMulti(
                        new(() => task.IsCompleted),
                        new(() => TaskMoveToHouse.UseSprint(false)),
                        new(() => P.FollowPath.Move([.. task.Result, .. appendMovement], true)),
                        new(() => P.FollowPath.Waypoints.Count == 0)
                        );
                });
            }
        }
        else if(Kind == CustomAliasKind.Teleport_to_Aetheryte)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
            P.TaskManager.Enqueue(() =>
            {
                var aetheryte = Svc.Data.GetExcelSheet<Aetheryte>().GetRow(Aetheryte);
                var nearestAetheryte = Svc.Objects.OrderBy(Player.DistanceTo).FirstOrDefault(x => x.IsTargetable && x.IsAetheryte() && Utils.IsAetheryteEligibleForCustomAlias(x));
                if(nearestAetheryte == null || P.Territory != aetheryte.Territory.RowId || Player.DistanceTo(nearestAetheryte) > SkipTeleport)
                {
                    P.TaskManager.InsertMulti(
                        new((Action)(() => S.TeleportService.TeleportToAetheryte(Aetheryte))),
                        new(() => !IsScreenReady()),
                        new(() => IsScreenReady())
                        );
                }
            });
        }
        else if(Kind == CustomAliasKind.Use_Aethernet)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable, "Wait until screen ready");
            P.TaskManager.Enqueue(() =>
            {
                P.TaskManager.InsertStack(() =>
                {
                    var aethernetPoint = Utils.GetAethernetNameWithOverrides(Aetheryte);
                    TaskTryTpToAethernetDestination.Enqueue(aethernetPoint);
                });
            }, "Teleport to aethernet destination");
            P.TaskManager.Enqueue(() => !IsScreenReady(), "Wait until screen is not ready");
            P.TaskManager.Enqueue(IsScreenReady, "Wait until screen is ready");
        }
        else if(Kind == CustomAliasKind.Circular_movement)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
            P.TaskManager.Enqueue(() => TaskMoveToHouse.UseSprint(false));
            P.TaskManager.Enqueue(() => P.FollowPath.Move([.. MathHelper.CalculateCircularMovement(CenterPoint, Player.Position.ToVector2(), CircularExitPoint.ToVector2(), out _, Precision, Tolerance, Clamp).Select(x => x.ToVector3(Player.Position.Y)).ToList(), .. (Vector3[])(WalkToExit ? [CircularExitPoint] : []), .. appendMovement], true));
            P.TaskManager.Enqueue(() => P.FollowPath.Waypoints.Count == 0);
        }
        else if(Kind == CustomAliasKind.Interact)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable && Utils.DismountIfNeeded());
            P.TaskManager.EnqueueTask(NeoTasks.InteractWithObject(() => Svc.Objects.OrderBy(Player.DistanceTo).FirstOrDefault(x => x.IsTargetable && x.DataId == DataID)));
        }
        else if(Kind == CustomAliasKind.Mount_Up)
        {
            if(!MountUpConditional || C.UseMount)
            {
                P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
                P.TaskManager.Enqueue(TaskMount.MountIfCan);
            }
        }
        else if(Kind == CustomAliasKind.Select_Yes)
        {
            P.TaskManager.Enqueue(() =>
            {
                if(StopOnScreenFade && !IsScreenReady()) return true;
                if(TryGetAddonMaster<AddonMaster.SelectYesno>(out var m) && m.IsAddonReady)
                {
                    //PluginLog.Debug($"Parsed text: [{m.Text}], options: {SelectOption.Where(x => x.Length > 0).Select(Utils.ParseSheetPattern).Print("\n")}");
                    if(m.Text.ContainsAny(SelectOption.Where(x => x.Length > 0).Select(Utils.ParseSheetPattern)) && EzThrottler.Throttle($"CustomCommandSelectYesno_{ID}", 200))
                    {
                        m.Yes();
                        return true;
                    }
                }
                return false;
            }, new(abortOnTimeout: false, timeLimitMS: 10000));
        }
        else if(Kind == CustomAliasKind.Select_List_Option)
        {
            P.TaskManager.Enqueue(() =>
            {
                if(StopOnScreenFade && !IsScreenReady()) return true;
                {
                    if(TryGetAddonMaster<AddonMaster.SelectString>(out var m) && m.IsAddonReady)
                    {
                        if(Utils.TryFindEqualsOrContains(m.Entries, e => e.Text, SelectOption.Where(x => x.Length > 0).Select(Utils.ParseSheetPattern), out var e) && EzThrottler.Throttle($"CustomCommandSelectString_{ID}", 200))
                        {
                            e.Select();
                            return true;
                        }
                    }
                }
                {
                    if(TryGetAddonMaster<AddonMaster.SelectIconString>(out var m) && m.IsAddonReady)
                    {
                        if(Utils.TryFindEqualsOrContains(m.Entries, e => e.Text, SelectOption.Where(x => x.Length > 0).Select(Utils.ParseSheetPattern), out var e) && EzThrottler.Throttle($"CustomCommandSelectString_{ID}", 200))
                        {
                            e.Select();
                            return true;
                        }
                    }
                }
                return false;
            }, new(abortOnTimeout: false, timeLimitMS: 10000));
        }
        else if(Kind == CustomAliasKind.Confirm_Contents_Finder)
        {
            P.TaskManager.Enqueue((Action)(() => EzThrottler.Throttle($"CustomCommandCFCConfirm_{ID}", 1000, true)));
            P.TaskManager.Enqueue(() =>
            {
                if(StopOnScreenFade && !IsScreenReady()) return true;
                if(TryGetAddonMaster<AddonMaster.ContentsFinderConfirm>(out var m) && m.IsAddonReady)
                {
                    if(EzThrottler.Throttle($"CustomCommandCFCConfirm_{ID}", 2000))
                    {
                        m.Commence();
                        return true;
                    }
                }
                return false;
            }, new(abortOnTimeout: false, timeLimitMS: 20000));
        }
    }
}
