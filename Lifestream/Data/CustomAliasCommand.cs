using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
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
    internal int ChainGroup = 0;
    public CustomAliasKind Kind;
    public Vector3 Point;
    public List<Vector3> ExtraPoints = [];
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
    public bool RequireTerritoryChange = false;
    public uint Territory = 0;
    public float? InteractDistance = null;

    public bool ShouldSerializeInteractDistance() => Kind.EqualsAny(CustomAliasKind.Interact) && InteractDistance != Default.InteractDistance;
    public bool ShouldSerializeWalkToExit() => Kind.EqualsAny(CustomAliasKind.Circular_movement) && WalkToExit != Default.WalkToExit;
    public bool ShouldSerializeExtraPoints() => ExtraPoints.Count > 0;
    public bool ShouldSerializeTerritory() => Territory != 0 && Kind.EqualsAny(CustomAliasKind.Move_to_point, CustomAliasKind.Navmesh_to_point, CustomAliasKind.Circular_movement);
    public bool ShouldSerializeRequireTerritoryChange() => Kind.EqualsAny(CustomAliasKind.Wait_for_Transition);
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
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable, $"{Kind}: Wait for screen and player interactable");
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
            var selectedPoint = GenericHelpers.GetRandom([Point, .. ExtraPoints]);
            P.TaskManager.Enqueue(() => Territory == 0 || Territory == Player.Territory, $"{Kind}: Wait for selected territory");
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable, $"{Kind}: Wait for screen and player interactable");
            if(UseFlight) P.TaskManager.Enqueue(FlightTasks.FlyIfCan, $"{Kind}: Fly if can");
            P.TaskManager.Enqueue(() => TaskMoveToHouse.UseSprint(false), $"{Kind}: use sprint");
            P.TaskManager.Enqueue(() => P.FollowPath.Move([selectedPoint.Scatter(Scatter), .. appendMovement], true), $"{Kind}: Enqueue move");
            P.TaskManager.Enqueue(WaitForMoveEndOrOccupied, $"{Kind}: Wait until move ends/occupied", new(timeLimitMS:5.Minutes()));
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable, $"{Kind}: Wait for screen and player interactable");
        }
        else if(Kind == CustomAliasKind.Navmesh_to_point)
        {
            P.TaskManager.Enqueue(() => Territory == 0 || Territory == Player.Territory, $"{Kind}: Wait for selected territory");
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable && S.Ipc.VnavmeshIPC.IsReady() == true, $"{Kind}: Wait for screen and player interactable and vnav ready", new(timeLimitMS: 5 * 60 * 1000));
            if(UseTA && Svc.PluginInterface.InstalledPlugins.Any(x => x.Name == "TextAdvance" && x.IsLoaded))
            {
                P.TaskManager.Enqueue(() =>
                {
                    S.Ipc.TextAdvanceIPC.EnqueueMoveTo2DPoint(new()
                    {
                        Position = Point,
                        NoInteract = true,
                    }, 5f);
                }, $"{Kind}: Move to 2d point via TX");
                P.TaskManager.Enqueue(S.Ipc.TextAdvanceIPC.IsBusy, $"{Kind}: wait until movement starts", new(abortOnTimeout: false, timeLimitMS: 5000));
                P.TaskManager.Enqueue(() => !S.Ipc.TextAdvanceIPC.IsBusy(), $"{Kind}: wait until movement ends", new(timeLimitMS: 5.Minutes()));
            }
            else
            {
                if(UseFlight) P.TaskManager.Enqueue(FlightTasks.FlyIfCan);
                P.TaskManager.Enqueue(() =>
                {
                    var task = S.Ipc.VnavmeshIPC.Pathfind(Player.Position, Point, UseFlight);
                    P.TaskManager.InsertMulti(
                        new(() => task?.IsCompleted == true, new(timeLimitMS:5.Minutes())),
                        new(() => TaskMoveToHouse.UseSprint(false)),
                        new(() => P.FollowPath.Move([.. task.Result, .. appendMovement], true)),
                        new(() => P.FollowPath.Waypoints.Count == 0, new(timeLimitMS:5.Minutes()))
                        );
                });

                P.TaskManager.Enqueue(() => P.FollowPath.Move([.. appendMovement], true), $"{Kind}: Move");
                P.TaskManager.Enqueue(WaitForMoveEndOrOccupied, $"{Kind}: Wait until move ends/occupied");
                P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable, $"{Kind}: Wait for screen and player interactable");
            }
        }
        else if(Kind == CustomAliasKind.Teleport_to_Aetheryte)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable, $"{Kind}: Wait for screen and player interactable");
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
            }, $"{Kind}: Teleport to aetheryte {Aetheryte}");
        }
        else if(Kind == CustomAliasKind.Use_Aethernet)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable, $"{Kind}: Wait for screen and player interactable");
            P.TaskManager.Enqueue(() =>
            {
                P.TaskManager.InsertStack(() =>
                {
                    var aethernetPoint = Utils.GetAethernetNameWithOverrides(Aetheryte);
                    TaskTryTpToAethernetDestination.Enqueue(aethernetPoint);
                });
            }, $"{Kind}: Teleport to aethernet destination");
            P.TaskManager.Enqueue(() => !IsScreenReady(), $"{Kind}: Wait until screen is not ready");
            P.TaskManager.Enqueue(IsScreenReady, $"{Kind}: Wait until screen is ready");
        }
        else if(Kind == CustomAliasKind.Circular_movement)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
            P.TaskManager.Enqueue(() => TaskMoveToHouse.UseSprint(false));
            P.TaskManager.Enqueue(() => P.FollowPath.Move([.. MathHelper.CalculateCircularMovement(CenterPoint, Player.Position.ToVector2(), CircularExitPoint.ToVector2(), out _, Precision, Tolerance, Clamp).Select(x => x.ToVector3(Player.Position.Y)).ToList(), .. (Vector3[])(WalkToExit ? [CircularExitPoint] : []), .. appendMovement], true));
            P.TaskManager.Enqueue(() => P.FollowPath.Waypoints.Count == 0, new(timeLimitMS:5.Minutes()));
        }
        else if(Kind == CustomAliasKind.Interact)
        {
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable && Utils.DismountIfNeeded());
            IGameObject selector() => Svc.Objects.OrderBy(Player.DistanceTo).FirstOrDefault(x => x.IsTargetable && x.DataId == DataID);
            if(InteractDistance != null)
            {
                P.TaskManager.EnqueueTask(NeoTasks.ApproachObjectViaAutomove(selector, this.InteractDistance.Value));
            }
            P.TaskManager.EnqueueTask(NeoTasks.InteractWithObject(selector));
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
            var gId = Guid.NewGuid();
            P.TaskManager.Enqueue(() =>
            {
                if(StopOnScreenFade && !IsScreenReady()) return true;
                ref var clicked = ref Ref<bool>.Get($"{ID}_{gId}", false);
                var visible = false;
                {
                    if(TryGetAddonMaster<AddonMaster.SelectString>(out var m))
                    {
                        if(m.IsAddonReady)
                        {
                            if(Utils.TryFindEqualsOrContains(m.Entries, e => e.Text, SelectOption.Where(x => x.Length > 0).Select(Utils.ParseSheetPattern), out var e))
                            {
                                visible = true;
                                if(EzThrottler.Throttle($"CustomCommandSelectString_{ID}", 200))
                                {
                                    e.Select();
                                    clicked = true;
                                    return false;
                                }
                            }
                        }
                    }
                }
                {
                    if(TryGetAddonMaster<AddonMaster.SelectIconString>(out var m))
                    {
                        if(m.IsAddonReady)
                        {
                            if(Utils.TryFindEqualsOrContains(m.Entries, e => e.Text, SelectOption.Where(x => x.Length > 0).Select(Utils.ParseSheetPattern), out var e))
                            {
                                visible = true;
                                if(EzThrottler.Throttle($"CustomCommandSelectString_{ID}", 200))
                                {
                                    e.Select();
                                    clicked = true;
                                    return false;
                                }
                            }
                        }
                    }
                }
                return clicked && !visible;
            }, $"{Kind}: {SelectOption.Print()}", new(abortOnTimeout: false, timeLimitMS: 10000));
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
        else if(Kind == CustomAliasKind.Wait_for_Transition)
        {
            P.TaskManager.Enqueue(() =>
            {
                var territory = Svc.ClientState.TerritoryType;
                P.TaskManager.InsertStack(() =>
                {
                    P.TaskManager.Enqueue(() => !IsScreenReady(), $"{Kind}: Wait for screen not ready");
                    P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable && (!RequireTerritoryChange || Svc.ClientState.TerritoryType != territory), $"{Kind}: Wait for screen ready {ExcelTerritoryHelper.GetName(territory, true)}");
                });
            }, $"{Kind}: Wait for transition");
        }
    }

    private bool WaitForMoveEndOrOccupied()
    {
        if(Svc.Condition[ConditionFlag.Occupied33] || (Territory != 0 && Territory != Player.Territory))
        {
            P.FollowPath.Stop();
        }
        return P.FollowPath.Waypoints.Count == 0;
    }

    public unsafe bool CanExecute(out string error)
    {
        error = null;
        if(!Player.Available) return false;
        if(this.Kind == CustomAliasKind.Teleport_to_Aetheryte)
        {
            if(!Svc.AetheryteList.Any(x => x.AetheryteId == this.Aetheryte))
            {
                error = $"Aetheryte {Svc.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(this.Aetheryte)?.PlaceName.Value.Name ?? $"{this.Aetheryte}"} is not unlocked";
                return false;
            }
        }
        else if(this.Kind == CustomAliasKind.Use_Aethernet)
        {
            if(S.Data.DataStore.Aetherytes.Values.Any(x => x.Any(s => s.ID == this.Aetheryte)))
            {
                if(!UIState.Instance()->IsAetheryteUnlocked(this.Aetheryte))
                {
                    error = $"Aethernet shard {Svc.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(this.Aetheryte)?.PlaceName.Value.Name ?? $"{this.Aetheryte}"} is not unlocked";
                    return false;
                }
            }
            else if(this.Aetheryte == 70)
            {
                if(!UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(69208))
                {
                    error = $"Firmament is not unlocked";
                    return false;
                }
            }
        }
        else if(this.Kind == CustomAliasKind.Change_world)
        {
            if(!S.Data.DataStore.Worlds.Contains(ExcelWorldHelper.GetName(this.World)) && !S.Data.DataStore.DCWorlds.Contains(ExcelWorldHelper.GetName(this.World)))
            {
                error = $"Can not visit {ExcelWorldHelper.GetName(this.World)} from {Player.CurrentWorld}";
                return false;
            }
        }
        return true;
    }
}
