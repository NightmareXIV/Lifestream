﻿using Dalamud.Game;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Utility;
using ECommons.Automation;
using ECommons.Automation.UIInput;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.EzSharedDataManager;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.Reflection;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.AtkReaders;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.Schedulers;
using Lifestream.Systems.Legacy;
using Lifestream.Tasks;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.Utility;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using NightmareUI.ImGuiElements;
using Callback = ECommons.Automation.Callback;
using Path = System.IO.Path;

namespace Lifestream.GUI;

internal static unsafe class UIDebug
{
    internal static uint DebugTerritory = 0;
    internal static TinyAetheryte? DebugAetheryte = null;
    internal static int DC = 0;
    internal static int Destination = 0;
    internal static List<Vector3> DebugPath = [];
    internal static void Draw()
    {
        ImGuiEx.EzTabBar("debug",
            InternalLog.ImGuiTab(),
            ("Data editor", Editor, null, true),
            ("Housing data", Housing, null, true),
            ("AtkReader", Reader, null, true),
            ("Debug", Debug, null, true),
            ("Multipath", TabMultipath.Draw, null, true)
            );
    }

    private static int Resize = 60;
    private static int LastPlot = -1;
    private static bool doCurPlot = false;
    private static bool ShowPathes = false;
    private static bool ShowFirstPoint = true;
    private static int u;
    private static int v;
    private static List<Vector3> CurrentPath = null;
    private static bool DoAutotest = false;
    private static int AutotestPlot = 0;
    private static int AutotestWard = 30;
    private static ResidentialAetheryteKind AutotestKind = ResidentialAetheryteKind.Gridania;
    private static List<long> StuckRecords = [];
    private static Vector3? LastPosition = null;

    private static void Housing()
    {
        if(CurrentPath != null)
        {
            if(ImGui.Begin($"Lifestream Edit Path"))
            {
                if(ImGui.Button("Finish"))
                {
                    Svc.Framework.RunOnFrameworkThread(() =>
                    {
                        Utils.SaveGeneratedHousingData();
                    });
                    CurrentPath = null;
                }
                if(CurrentPath != null)
                {
                    UIHouseReg.DrawPathEditor(CurrentPath);
                }
            }
            ImGui.End();
        }
        if(ImGui.Button("Load from config folder"))
        {
            var d = EzConfig.LoadConfiguration<HousingData>("GeneratedHousingData.json", true);
            if(d != null) S.Data.ResidentialAethernet.HousingData = d;
        }
        var data = S.Data.ResidentialAethernet.HousingData.Data;
        if(ImGui.CollapsingHeader("Autotest"))
        {
            if(DoAutotest)
            {
                if(Utils.IsBusy())
                {
                    EzThrottler.Throttle("Autotest", 500, true);
                }
                else
                {
                    if(AutotestPlot >= 60)
                    {
                        DuoLog.Information("Autotest complete");
                        DoAutotest = false;
                    }
                    else if(EzThrottler.Throttle("Autotest"))
                    {
                        AutotestPlot++;
                        DuoLog.Information($"Now going to plot {AutotestPlot}");
                        TaskTpAndGoToWard.Enqueue(Player.CurrentWorld, AutotestKind, AutotestWard, AutotestPlot - 1, false, false);
                    }
                }
            }
            ImGui.Checkbox("Autotest active", ref DoAutotest);
            ImGuiEx.EnumCombo("Autotest aetheryte", ref AutotestKind);
            ImGui.InputInt("Autotest ward", ref AutotestWard);
            ImGui.InputInt("Autotest current plot", ref AutotestPlot);
            if(DoAutotest && EzThrottler.Throttle("StuckAutocheck", 1000))
            {
                if(P.FollowPath.Waypoints.Count > 0)
                {
                    if(LastPosition != null && Vector3.DistanceSquared(LastPosition.Value, Player.Position) < 1)
                    {
                        StuckRecords.Add(Environment.TickCount64);
                        StuckRecords.RemoveAll(x => Environment.TickCount64 - x > 10000);
                        if(StuckRecords.Count > 1)
                        {
                            DuoLog.Information($"Stuck at {AutotestPlot} - {AutotestKind}");
                            DoAutotest = false;
                            P.FollowPath.Stop();
                            Utils.TryNotify("Stuck");
                        }
                    }
                    LastPosition = Player.Position;
                }
                else
                {
                    LastPosition = null;
                }
            }
        }
        if(data.TryGetValue(P.Territory, out var plots))
        {
            if(ImGui.CollapsingHeader("Control"))
            {
                ImGui.Checkbox($"Show pathes", ref ShowPathes);
                ImGui.SameLine();
                ImGui.Checkbox("Show first point", ref ShowFirstPoint);
                if(ShowPathes)
                {
                    var aetheryte = S.Data.ResidentialAethernet.ActiveAetheryte ?? S.Data.ResidentialAethernet.GetFromIGameObject(Svc.Targets.Target);
                    if(aetheryte != null)
                    {
                        foreach(var x in plots)
                        {
                            if(x.AethernetID == aetheryte.Value.ID && x.Path.Count > 0)
                            {
                                S.Ipc.SplatoonManager.RenderPath(ShowFirstPoint ? x.Path : x.Path[1..]);
                            }
                        }
                    }
                }
                var curPlot = HousingManager.Instance()->GetCurrentPlot();
                if(curPlot != -1) LastPlot = curPlot;
                ImGuiEx.Text($"Plot: {curPlot + 1}");
                ImGui.SetNextItemWidth(150f.Scale());
                ImGui.InputInt($"Resize", ref Resize);
                ImGui.SameLine();
                if(ImGui.Button("Resize arrays"))
                {
                    while(plots.Count > Resize) plots.RemoveAt(plots.Count - 1);
                    while(plots.Count < Resize) plots.Add(new());
                }
                if(ImGui.Button("Begin path calculation"))
                {
                    Chat.ExecuteCommand("/clearlog");
                    var aetheryte = S.Data.ResidentialAethernet.ActiveAetheryte ?? S.Data.ResidentialAethernet.GetFromIGameObject(Svc.Targets.Target);
                    if(aetheryte != null)
                    {
                        P.TaskManager.Enqueue(() => S.Ipc.VnavmeshIPC.Rebuild());
                        P.TaskManager.Enqueue(() => S.Ipc.VnavmeshIPC.IsReady(), TaskSettings.TimeoutInfinite);
                        for(var i = 0; i < plots.Count; i++)
                        {
                            var x = plots[i];
                            if(x.AethernetID == aetheryte.Value.ID)
                            {
                                var index = i;
                                TaskGeneratePath.Enqueue(i, x);
                            }
                        }
                        for(var i = 0; i < plots.Count; i++)
                        {
                            var x = plots[i];
                            if(x.AethernetID != aetheryte.Value.ID && x.Path.Count > 0)
                            {
                                var index = i;
                                TaskGeneratePath.EnqueueValidate(i, x, aetheryte.Value);
                            }
                        }
                        P.TaskManager.Enqueue(() => P.NotificationMasterApi.DisplayTrayNotification("Path Completed"));
                    }
                }
                if(ImGui.Button($"For plot {LastPlot + 1}"))
                {
                    doCurPlot = true;
                }
            }
            List<ImGuiEx.EzTableEntry> entries = [];
            for(var i = 0; i < plots.Count; i++)
            {
                var index = i;
                var plot = plots[i];
                entries.Add(
                    new("Num", () => ImGuiEx.Text($"{index + 1}")),
                    new("Front", () => ImGuiEx.Text($"{plot.Front}")),
                    new("Aethernet", () => ImGuiEx.Text($"{Svc.Data.GetExcelSheet<HousingAethernet>().GetRowOrDefault(plot.AethernetID)?.PlaceName.ValueNullable?.Name ?? plot.AethernetID.ToString()}")),
                    new("Edit", () =>
                    {
                        if(ImGui.Button($"Edit{index + 1}"))
                        {
                            CurrentPath = plots[index].Path;
                        }
                    }),
                    new("Action", () =>
                    {
                        if(ImGui.Button($"Set{index + 1}") || (doCurPlot && index == LastPlot))
                        {
                            LastPlot = -1;
                            doCurPlot = false;
                            Chat.ExecuteCommand("/clearlog");
                            DuoLog.Information($"For plot {index + 1}");
                            plot.Front = Player.Object.Position;
                            var candidates = Svc.Objects.Where(x => x.DataId.EqualsAny(Utils.AethernetShards) && Vector3.Distance(plot.Front, x.Position) < 100f && S.Data.ResidentialAethernet.GetFromIGameObject(x) != null);
                            Task.Run(() =>
                            {
                                var currentDistance = float.MaxValue;
                                var currentAetheryte = -1;
                                foreach(var x in candidates)
                                {
                                    DuoLog.Information($"Candidate: {S.Data.ResidentialAethernet.GetFromIGameObject(x).Value.Name}");
                                    var path = S.Ipc.VnavmeshIPC.Pathfind(plot.Front, x.Position, false);
                                    path.Wait();
                                    if(path.Result != null)
                                    {
                                        var distance = Utils.CalculatePathDistance([.. path.Result]);
                                        DuoLog.Information($"-- Distance: {distance} - best: {distance < currentDistance}");
                                        if(distance < currentDistance)
                                        {
                                            currentDistance = distance;
                                            currentAetheryte = (int)S.Data.ResidentialAethernet.GetFromIGameObject(x).Value.ID;
                                        }
                                    }
                                    else
                                    {
                                        DuoLog.Information($"-- Failed to calculate distance");
                                    }
                                }
                                Svc.Framework.RunOnFrameworkThread(() =>
                                {
                                    plot.AethernetID = (uint)currentAetheryte;
                                    Utils.SaveGeneratedHousingData();
                                });
                            });
                        }
                    }),
                    new("Path", () =>
                    {
                        ImGuiEx.Text($"Points: {plot.Path.Count}, Distance: {Utils.CalculatePathDistance([Player.Object.Position, .. plot.Path])}");
                        if(ImGui.IsItemHovered())
                        {
                            S.Ipc.SplatoonManager.RenderPath(plot.Path);
                        }
                    })
                    );
            }
            if(ImGui.BeginChild("Table"))
            {
                ImGuiEx.EzTable(entries);
            }
            ImGui.EndChild();
        }
        else
        {
            if(ImGui.Button($"Create data for {ExcelTerritoryHelper.GetName(P.Territory)}"))
            {
                data[P.Territory] = [];
            }
        }
    }

    private static void Reader()
    {
        {
            if(TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
            {
                var r = new ReaderLobbyDKTWorldList(addon);
                ImGuiEx.Text($"Source: {r.Source}");
                ImGuiEx.Text($"Destination: {r.Destination}");
                foreach(var dc in r.Regions)
                {
                    ImGuiEx.Text($"  {dc.RegionTitle}");
                    foreach(var world in dc.DataCenters)
                    {
                        ImGuiEx.Text($"    {world.Id}/{world.Name}");
                    }
                }
            }
        }
        {
            if(TryGetAddonByName<AtkUnitBase>("TelepotTown", out var addon) && IsAddonReady(addon))
            {
                var reader = new ReaderTelepotTown(addon);
                for(var i = 0; i < reader.DestinationData.Count; i++)
                {
                    var data = reader.DestinationData[i];
                    var name = reader.DestinationName[i];
                    ImGuiEx.Text($"{data.Type}|{data.State}|{data.CallbackData}|{data.IconID}|{name.Name}");
                }
            }
        }
    }

    private static int index = 0;
    private static string str = "";
    private static string str2 = "";
    private static string str3 = "";
    private static string World = "";
    private static ResidentialAetheryteKind ResiA;
    private static int Ward = 1;
    private static Vector2 uv0;
    private static Vector2 uv1;
    private static Vector2 size;
    private static string addr = "";
    private static string CharaName = "";
    private static int WorldSel;

    private static void Debug()
    {
        if(ImGui.CollapsingHeader("Image test"))
        {
            if(ThreadLoadImageHandler.TryGetTextureWrap("https://github.com/FFXIV-CombatReborn/RotationSolverReborn/blob/main/Images/Logo.png?raw=true", out var tex))
            {
                ImGui.Image(tex.Handle, new(-1));
            }
        }
        if(ImGui.CollapsingHeader("IPC test - travel from chara select screen"))
        {
            ref var name = ref Ref<string>.Get("name");
            ref var world = ref Ref<string>.Get("world");
            ref var dest = ref Ref<string>.Get("dest");
            ref var nologin = ref Ref<bool>.Get("nologin");
            ImGui.InputText("Chara name", ref name, 100);
            ImGui.InputText("Chara world", ref world, 100);
            ImGui.InputText("Destination", ref dest, 100);
            ImGui.Checkbox("No login", ref nologin);
            ImGuiEx.Text($"CanInitiateTravelFromCharaSelectList: {S.Ipc.IPCProvider.CanInitiateTravelFromCharaSelectList()}");
            ImGuiEx.Text($"CanAutoLogin: {S.Ipc.IPCProvider.CanAutoLogin()}");
            if(ImGui.Button("ConnectAndOpenCharaSelect")) DuoLog.Information($"{S.Ipc.IPCProvider.ConnectAndOpenCharaSelect(name, world)}");
            if(ImGui.Button("InitiateTravelFromCharaSelectScreen")) DuoLog.Information($"{S.Ipc.IPCProvider.InitiateTravelFromCharaSelectScreen(name, world, dest, nologin)}");
            if(ImGui.Button("ConnectAndTravel")) DuoLog.Information($"{S.Ipc.IPCProvider.ConnectAndTravel(name, world, dest, nologin)}");
            if(ImGui.Button("ConnectAndLogin")) DuoLog.Information($"{S.Ipc.IPCProvider.ConnectAndLogin(name, world)}");
            if(ImGui.Button("InitiateLoginFromCharaSelectScreen")) DuoLog.Information($"{S.Ipc.IPCProvider.InitiateLoginFromCharaSelectScreen(name, world)}");
        }
        if(ImGui.CollapsingHeader("ApproachConditionIsMet"))
        {
            ImGuiEx.Text($"ApproachConditionIsMet: {Utils.ApproachConditionIsMet()}");
            ImGuiEx.Text($"IsAetheryte: {P.ActiveAetheryte?.IsAetheryte}");
            ImGuiEx.Text($"GetReachableAetheryte: {Utils.GetReachableAetheryte(x => x.IsAetheryte())}");
        }
        if(ImGui.CollapsingHeader("S.Data.DataStore.Aetherytes"))
        {
            foreach(var x in S.Data.DataStore.Aetherytes)
            {
                ImGuiEx.Text($"{x.Key.Name} ({Svc.Data.GetExcelSheet<Aetheryte>(ClientLanguage.English).GetRowOrDefault(x.Key.ID).Value.AethernetName.Value.Name.GetText()})");
                ImGui.Indent();
                ImGuiEx.Text($"{x.Value.Select(s => $"{s.Name} ({Svc.Data.GetExcelSheet<Aetheryte>(ClientLanguage.English).GetRowOrDefault(s.ID).Value.AethernetName.Value.Name.GetText()})").Print("\n")}");
                ImGui.Unindent();
            }
        }
        if(ImGui.CollapsingHeader("Agent Map debug"))
        {
            if(TryGetAddonByName<AddonAreaMap>("AreaMap", out var addon))
            {
                ImGuiEx.Text($"{addon->HoveredCoords} - press ctrl to copy");
                if(ImGuiEx.Ctrl && EzThrottler.Throttle("Copy") && !CSFramework.Instance()->WindowInactive)
                {
                    Copy($", new({addon->HoveredCoords.X}f, {addon->HoveredCoords.Y}f)");
                }
            }
        }
        if(ImGui.CollapsingHeader("IPC debug"))
        {
            ref var id = ref Ref<int>.Get("aetheryteId");
            ImGui.InputInt("aetheryte id", ref id);
            if(ImGui.Button("AethernetTeleportById")) DuoLog.Information($"{S.Ipc.IPCProvider.AethernetTeleportById((uint)id)}");
            if(ImGui.Button("HousingAethernetTeleportById")) DuoLog.Information($"{S.Ipc.IPCProvider.HousingAethernetTeleportById((uint)id)}");
            if(ImGui.Button("AethernetTeleportByPlaceNameId")) DuoLog.Information($"{S.Ipc.IPCProvider.AethernetTeleportByPlaceNameId((uint)id)}");
            if(ImGui.Button("AethernetTeleportToFirmament")) DuoLog.Information($"{S.Ipc.IPCProvider.AethernetTeleportToFirmament()}");
            if(ImGui.Button("GetActiveAetheryte")) DuoLog.Information($"{S.Ipc.IPCProvider.GetActiveAetheryte()}");
            if(ImGui.Button("GetActiveResidentialAetheryte")) DuoLog.Information($"{S.Ipc.IPCProvider.GetActiveResidentialAetheryte()}");
        }
        ImGuiEx.Text($"Active aetheryte: {P.ActiveAetheryte}");
        if(ImGui.CollapsingHeader("Chat"))
        {
            if(ImGui.Button("Send message (echo)")) Chat.ExecuteCommand($"/e Test test test {Random.Shared.Next()}");
            if(ImGui.Button("Send message (current channel)")) Chat.SendMessage($"Password: {Random.Shared.Next()}");
            if(ImGui.Button("Use sprint")) Chat.ExecuteAction(3);
            if(ImGui.Button("Use jump")) Chat.ExecuteGeneralAction(2);
            try
            {
                if(ImGui.Button("Try invalid string")) Chat.ExecuteCommand("/e \u000012345");
            }
            catch(Exception e)
            {
                e.Log();
            }
        }
        ImGui.Text(Utils.ParseSheetPattern("<Addon:10:Text>"));
        ImGui.Text(Utils.ParseSheetPattern("<Addon:10:RowId>"));
        if(ImGui.CollapsingHeader("DawnStory"))
        {
            if(TryGetAddonMaster<AddonMaster.DawnStory>(out var m) && m.IsAddonReady)
            {
                ImGuiEx.Text($"Cnt: {m.Reader.EntryCount}");
                foreach(var x in m.Entries)
                {
                    ImGuiEx.Text($"{x.Name} / {x.ReaderEntryName.Level} / {x.ReaderEntry.Callback} / {x.Index}");
                    if(ImGuiEx.HoveredAndClicked() && x.Status != 2)
                    {
                        x.Select();
                    }
                }
            }
        }
        if(ImGui.CollapsingHeader("ReaderLobbyDKTWorldList"))
        {
            if(TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
            {
                var r = new ReaderLobbyDKTWorldList(addon);
                ImGuiEx.Text($"""
                    Source {r.Source}
                    Destination {r.Destination}
                    SelectedDataCenter {r.SelectedDataCenter}
                    """);
                ImGuiEx.Text($"Regions:");
                ImGui.Indent();
                foreach(var region in r.Regions)
                {
                    ImGuiEx.Text($"""
                        {region.RegionTitle}
                        """);
                    ImGuiEx.Text("DataCenters");
                    foreach(var dc in region.DataCenters)
                    {
                        ImGui.Indent();
                        ImGuiEx.Text($"""
                            {dc.Name}
                            """);
                        ImGui.Unindent();
                    }
                }
                ImGui.Separator();
                ImGuiEx.Text($"Worlds: {r.GetNumWorlds()}");
                ImGui.Indent();
                foreach(var x in r.Worlds)
                {
                    ImGuiEx.Text($"{x.WorldName}, active={x.IsAvailable}");
                }
                ImGui.Unindent();
                ImGui.Unindent();
            }
        }
        if(ImGui.CollapsingHeader("Context"))
        {
            if(TryGetAddonMaster<AddonMaster.ContextMenu>(out var m))
            {
                foreach(var e in m.Entries)
                {
                    ImGuiEx.Text($"{e.Text} / {e.Enabled}");
                }
            }
        }
        if(ImGui.CollapsingHeader("CharaSelect"))
        {
            if(TryGetAddonMaster<AddonMaster._CharaSelectListMenu>(out var m))
            {
                foreach(var x in m.Characters)
                {
                    ImGuiEx.Text($"{x.Name}/{x.CurrentWorld}/{x.HomeWorld}/{x.IsSelected}");
                }
            }
        }
        if(ImGui.CollapsingHeader("Custom aethernet"))
        {
            if(ImGui.Button("Copy target") && Svc.Targets.Target != null)
            {
                var pname = TerritoryInfo.Instance()->AreaPlaceNameId;
                var pname2 = TerritoryInfo.Instance()->SubAreaPlaceNameId;
                Copy($"""
                    new(new({Svc.Targets.Target.Position.X:F1}f, {Svc.Targets.Target.Position.Z:F1}f), {P.Territory}, GetPlaceName({pname}), Base), //{Svc.Data.GetExcelSheet<PlaceName>().GetRowOrDefault(pname)?.Name.GetText()} ({pname}), {Svc.Data.GetExcelSheet<PlaceName>().GetRowOrDefault(pname2)?.Name.GetText()} ({pname2}), 
                    """);
            }
            ImGuiEx.Text($"Active: {S.Data.CustomAethernet.ActiveAetheryte}");
            ImGuiEx.Text($"Valid: {Utils.GetValidAetheryte()}");
            if(Utils.GetValidAetheryte() != null) ImGuiEx.Text($"FromIGameObject: {S.Data.CustomAethernet.GetFromIGameObject(Utils.GetValidAetheryte())}");
        }
        if(ImGui.Button("Get file list")) Utils.ReadClipboardFiles();
        if(ImGui.Button("Open PF self"))
        {
            S.Memory.OpenPartyFinderInfoDetour(AgentModule.Instance()->GetAgentByInternalId(AgentId.LookingForGroup), Player.CID);
        }
        if(ImGui.CollapsingHeader("Lobby2"))
        {
            if(TryGetAddonMaster<AddonMaster._CharaSelectListMenu>(out var m))
            {
                foreach(var x in m.Characters)
                {
                    ImGuiEx.Text($"{x.Name} is at {ExcelWorldHelper.GetName(x.Entry->CurrentWorldId)}/{x.IsVisitingAnotherDC}/{x.Entry->LoginFlags}");
                }
            }
        }
        if(ImGui.CollapsingHeader("Curcular movelemt"))
        {
            ImGuiEx.Text($"{MathHelper.IsPointPerpendicularToLineSegment(Player.Position.ToVector2(), new(-135f, -85f), new(-125.000f, -80f))}");
            ImGuiEx.Text($"{MathHelper.FindClosestPointOnLine(Player.Position.ToVector2(), new(-135f, -85f), new(-125.000f, -80f))}");
            ImGuiEx.Text($"{Vector2.Distance(Player.Position.ToVector2(), MathHelper.FindClosestPointOnLine(Player.Position.ToVector2(), new(-135f, -85f), new(-125.000f, -80f)))}");
            ref var target = ref Ref<Vector3>.Get();
            ref var exit = ref Ref<Vector3>.Get("exit");
            ref var list = ref Ref<List<Vector3>>.Get();
            ref var listList = ref Ref<List<List<Vector3>>>.Get();
            ref var prec = ref Ref<float>.Get("precision");
            ref var tol = ref Ref<int>.Get("tlr");
            ref var lim1 = ref Ref<float>.Get("lim1");
            ref var lim2 = ref Ref<float>.Get("lim2");
            ref var auto = ref Ref<bool>.Get("autocalc");
            if(ImGui.Button("Import"))
            {
                try
                {
                    var des = JsonConvert.DeserializeObject<CustomAliasCommand>(Paste());
                    target = des.CenterPoint.ToVector3();
                    exit = des.CircularExitPoint;
                    prec = des.Precision;
                    tol = des.Tolerance;
                    lim1 = des.Clamp?.Min ?? 0;
                    lim2 = des.Clamp?.Max ?? 0;
                }
                catch(Exception e)
                {
                    e.LogDuo();
                }
            }
            if(ImGui.Button("Set target")) target = Svc.Targets.Target?.Position ?? default;
            ImGui.SameLine();
            ImGuiEx.Text($"{target}");
            if(ImGui.Button("Set exit")) exit = Player.Position;
            ImGui.SameLine();
            ImGuiEx.Text($"{exit}");
            ImGui.InputFloat("Precision", ref prec);
            ImGui.InputInt("Tolerance", ref tol);
            ImGui.InputFloat("Limit1", ref lim1);
            ImGui.InputFloat("Limit2", ref lim2);
            if(ImGui.Button("Calculate") || (auto && EzThrottler.Throttle("AutoRec", 100)))
            {
                (float, float)? lmt = lim2 > lim1 ? (lim1, lim2) : null;
                list = MathHelper.CalculateCircularMovement(target, Player.Position, exit, out listList, prec, tol, lmt);
            }
            ImGui.SameLine();
            ImGui.Checkbox("AutoCalc", ref auto);
            if(list != null)
            {
                ImGuiEx.Text($"List: {list.Print()}");
                S.Ipc.SplatoonManager.RenderPath(list, false, true);
            }
            if(listList != null)
            {
                foreach(var x in listList)
                {
                    ImGuiEx.Text($"Candidate: {x.Print()}");
                    if(ImGui.IsItemHovered())
                    {
                        S.Ipc.SplatoonManager.RenderPath(x, false, true);
                    }
                }
            }
        }
        if(ImGui.CollapsingHeader("CharaSelectListMenu"))
        {
            var list = RaptureAtkUnitManager.Instance()->FocusedUnitsList;
            foreach(var x in list.Entries)
            {
                if(x.Value == null) continue;
                ImGuiEx.Text($"{x.Value->NameString}");
            }
            { if(TryGetAddonMaster<AddonMaster._CharaSelectListMenu>(out var m)) ImGuiEx.Text($"Selected chara: {m.Characters.FirstOrDefault(x => x.IsSelected)?.Name}"); }
        }
        ImGui.Checkbox("DisableHousePathData", ref P.DisableHousePathData);
        if(ImGui.CollapsingHeader("HUD"))
        {
            var hud = AgentHUD.Instance();
            for(var i = 0; i < hud->MapMarkers.Count; i++)
            {
                var marker = hud->MapMarkers[i];
                var pos = new Vector3(marker.Position.X, marker.Position.Y, marker.Position.Z);
                ImGuiEx.Text($"Marker {marker.IconId}, pos: {pos:F1}, distance: {Vector3.Distance(Player.Position, pos):f1}");
                if(ThreadLoadImageHandler.TryGetIconTextureWrap(marker.IconId, false, out var w))
                {
                    ImGui.SameLine();
                    ImGui.Image(w.Handle, new(30f));
                }
            }
        }
        var data = Svc.Data.GetExcelSheet<Addon>().GetRow(195);
        var text = data.Text.GetText();
        if(ImGui.Button("Lumina"))
        {
            /*foreach(var x in data.Text.Payloads)
            {
                PluginLog.Information($"Payload {x.PayloadType}, text: {x.ToString()}");
            }*/
        }
        if(ImGui.Button("Dalamud"))
        {
            foreach(var x in data.Text.ToDalamudString().Payloads)
            {
                PluginLog.Information($"Payload {x.Type}, text: {x.ToString()}");
            }
        }
        if(ImGui.Button("YesNo"))
        {
            if(TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon))
            {
                foreach(var x in addon->PromptText->NodeText.Read().Payloads)
                {
                    PluginLog.Information($"Payload {x.Type}, text: {x.ToString()}");
                }
            }
        }
        ImGui.InputText("##copyaddon", ref text, 300);
        if(ImGui.CollapsingHeader("Misc"))
        {
            if(ImGui.Button("Switch"))
            {
                bool Do()
                {
                    if(TryGetAddonByName<AddonRepair>("Repair", out var addon) && addon->AtkUnitBase.IsVisible)
                    {
                        var fwdBtn = addon->AtkUnitBase.GetNodeById(14)->GetAsAtkComponentButton();
                        fwdBtn->ClickAddonButton((AtkComponentBase*)addon, 2, EventType.CHANGE);

                        return true;

                    }
                    return false;
                }
                for(var i = 0; i < 10000; i++)
                {
                    P.TaskManager.Enqueue(Do);
                }
            }
        }
        if(ImGui.CollapsingHeader("Instance"))
        {
            ImGuiEx.Text($"""
                Max instances: {*S.Memory.MaxInstances}
                Initialized: {S.InstanceHandler.InstancesInitizliaed(out var maxInstances)} {maxInstances}
                GetInstance: {S.InstanceHandler.GetInstance()}
                DrawConditions: {S.Gui.Overlay.DrawConditions()}
                """);
            if(ImGui.Button("instance data reset")) C.PublicInstances.Clear();
            if(ImGui.Button("game version reset")) C.GameVersion = "";
        }
        ImGuiEx.Text($"Player interactable: {Player.Interactable}");
        ImGuiEx.Text($"Is moving: {AgentMap.Instance()->IsPlayerMoving}");
        ImGuiEx.Text($"IsOccupied: {IsOccupied()}");
        ImGuiEx.Text($"Casting: {Player.Object?.IsCasting}");
        if(ImGui.CollapsingHeader("Data test"))
        {
            foreach(var x in S.Data.DataStore.Aetherytes)
            {
                ImGuiEx.Text($"""
                    Key:
                        Name: {x.Key.Name}
                        ID: {x.Key.ID}
                        Pos: {x.Key.Position}
                        Group: {x.Key.Group}
                        Territory: {ExcelTerritoryHelper.GetName(x.Key.TerritoryType, true)}
                    Value:
                        Cnt: {x.Value.Count}
                    """);
                foreach(var z in x.Value)
                {
                    ImGui.Indent();
                    ImGuiEx.Text($"""
                        Name: {z.Name}
                        ID: {z.ID}
                        Pos: {z.Position}
                        Group: {z.Group}
                        Territory: {ExcelTerritoryHelper.GetName(z.TerritoryType, true)}
                        """);
                    ImGui.Unindent();
                }
            }
        }
        if(ImGui.CollapsingHeader("Lobby test"))
        {
            ImGui.InputText("Chara name", ref CharaName, 100);
            WorldSelector.Instance.Draw(ref WorldSel);
            if(ImGui.Button("Select"))
            {
                DCChange.SelectCharacter(CharaName, (uint)WorldSel);
            }
            if(ImGui.Button("Context"))
            {
                DCChange.OpenContextMenuForChara(CharaName, (uint)WorldSel, (uint)WorldSel);
            }
            var agent = AgentLobby.Instance();
            ImGuiEx.Text($"Active: {agent->IsAgentActive()}");
            for(var i = 0; i < agent->LobbyData.CharaSelectEntries.Count; i++)
            {
                var c = agent->LobbyData.CharaSelectEntries[i].Value;
                ImGuiEx.Text($"Locked: {agent->TemporaryLocked}");
                ImGuiEx.Text($"{i}: {c->Name.Read()}/{c->HomeWorldName.Read()}");
            }
        }
        if(ImGui.CollapsingHeader("Addon test"))
        {
            if(TryGetAddonByName<AddonSelectString>("SelectString", out var addon))
            {
                ImGuiEx.Text($"Entries: {addon->PopupMenu.PopupMenu.EntryCount}");
                foreach(var entry in new AddonMaster.SelectString(addon).Entries)
                {
                    ImGuiEx.Text($"{entry.Text}");
                    if(ImGuiEx.HoveredAndClicked())
                    {
                        entry.Select();
                    }
                }
            }
        }
        if(ImGui.Button("Refresh color"))
        {
            DalamudReflector.GetService("Dalamud.Plugin.Ipc.Internal.DataShare").GetFoP<System.Collections.IDictionary>("caches").Remove("ECommonsPatreonBannerRandomColor");
            ((System.Collections.IDictionary)typeof(EzSharedData).GetFieldPropertyUnion("Cache", ReflectionHelper.AllFlags).GetValue(null)).Remove("ECommonsPatreonBannerRandomColor");
        }
        if(ImGui.CollapsingHeader("Render"))
        {

            if(ImGui.Button("Save")) Svc.Data.GetFile("ui/uld/Teleport_hr1.tex").SaveFile("d:\\file.tex");
        }
        if(ImGui.CollapsingHeader("Housing manager"))
        {
            var h = HousingManager.Instance();
            if(h == null)
            {
                ImGuiEx.Text("null");
            }
            else
            {
                ImGuiEx.Text($"Ward: {h->GetCurrentWard()}");
                ImGuiEx.Text($"Plot: {h->GetCurrentPlot()}");
                ImGuiEx.Text($"Division: {h->GetCurrentDivision()}");
            }
        }
        if(ImGui.CollapsingHeader("Path"))
        {
            if(ImGui.Button("Add")) DebugPath.Add(Player.Object.Position);
            //if (ImGui.Button("Go")) P.FollowPath.Waypoints.AddRange(Enumerable.Reverse(DebugPath));
            if(ImGui.Button("Copy")) Copy($"new Vector3({Player.Object.Position.X}f, {Player.Object.Position.Y}f, {Player.Object.Position.Z}f);");
            for(var i = 0; i < DebugPath.Count; i++)
            {
                ImGuiEx.Text($"{DebugPath[i]}");
                if(ImGuiEx.HoveredAndClicked())
                {
                    DebugPath.RemoveAt(i);
                    break;
                }
            }
        }
        if(ImGui.CollapsingHeader("TPW"))
        {
            ImGui.InputText("World", ref World, 100);
            ImGuiEx.EnumCombo("Resi", ref ResiA);
            ImGui.InputInt("Ward", ref Ward);
            if(ImGui.Button("Go"))
            {
                TaskTpAndGoToWard.Enqueue(World, ResiA, Ward, 1, false, false);
            }
        }
        if(ImGui.CollapsingHeader("State"))
        {
            ImGuiEx.Text($"CanUseAetheryte = {Utils.CanUseAetheryte()}");
            ImGuiEx.Text($"ResidentialAethernet.ActiveAetheryte = {S.Data.ResidentialAethernet.ActiveAetheryte}");
            ImGuiEx.Text($"GetValidAetheryte = {Utils.GetValidAetheryte()}");
        }
        if(ImGui.CollapsingHeader("Housing aethernet"))
        {
            foreach(var x in S.Data.ResidentialAethernet.ZoneInfo)
            {
                if(ImGuiEx.TreeNode($"{x}"))
                {
                    foreach(var a in x.Value.Aetherytes)
                    {
                        ImGuiEx.Text($"{a.Name} / {a.Position} / {ExcelTerritoryHelper.GetName(a.TerritoryType)}");
                    }
                    ImGui.TreePop();
                }
            }
        }
        if(ImGui.CollapsingHeader("DCV"))
        {
            if(ImGui.Button("Unlock all worlds")) UnlockAllWorlds();
            if(ImGui.Button("Enable AtkComponentTreeList_vf31Hook hook"))
            {
                S.Memory.AtkComponentTreeList_vf31Hook.Enable();
            }
            {
                if(TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && ImGui.Button("Try event"))
                {
                    //S.Memory.ConstructEvent(addon);
                    ImGuiEx.Text($"PTR: {(nint)(addon->UldManager.NodeList[7]->GetAsAtkComponentList() + 456):X16}");
                }
            }
            if(ImGui.Button($"{nameof(DCChange.Logout)}")) PluginLog.Information($"{DCChange.Logout()}");
            if(ImGui.Button($"{nameof(DCChange.SelectYesLogout)}")) PluginLog.Information($"{DCChange.SelectYesLogout()}");
            if(ImGui.Button($"Enable AddonDKTWorldCheck_ReceiveEventHook")) S.Memory.AddonDKTWorldList_ReceiveEventHook.Enable();
            if(ImGui.Button($"{nameof(DCChange.TitleScreenClickStart)}")) PluginLog.Information($"{DCChange.TitleScreenClickStart()}");
            //if (ImGui.Button($"{nameof(DCChange.OpenContextMenuForChara)}")) PluginLog.Information($"{DCChange.OpenContextMenuForChara(str)}");
            ImGui.SameLine();
            ImGui.InputText($"Chara name", ref str, 100);
            if(ImGui.Button($"{nameof(DCChange.SelectVisitAnotherDC)}")) PluginLog.Information($"{DCChange.SelectVisitAnotherDC()}");
            if(ImGui.Button($"{nameof(DCChange.SelectTargetDataCenter)}")) PluginLog.Information($"{DCChange.SelectTargetDataCenter(str2)}");
            ImGui.SameLine();
            ImGui.InputText($"dc name", ref str2, 100);
            if(ImGui.Button($"{nameof(DCChange.SelectTargetWorld)}")) PluginLog.Information($"{DCChange.SelectTargetWorld(str3, null)}");
            ImGui.SameLine();
            ImGui.InputText($"w name", ref str3, 100);
            if(ImGui.Button($"{nameof(DCChange.ConfirmDcVisit)}")) PluginLog.Information($"{DCChange.ConfirmDcVisit()}");
            if(ImGui.Button($"{nameof(DCChange.ConfirmDcVisit2)}")) PluginLog.Information($"{DCChange.ConfirmDcVisit2(default, default, default, default, default)}");
            if(ImGui.Button($"{nameof(DCChange.SelectOk)}")) PluginLog.Information($"{DCChange.SelectOk()}");
            if(ImGui.Button($"{nameof(DCChange.ConfirmDcVisitIntention)}")) PluginLog.Information($"{DCChange.ConfirmDcVisitIntention()}");
            if(ImGui.Button($"{nameof(DCChange.SelectYesLogin)}")) PluginLog.Information($"{DCChange.SelectYesLogin()}");
            ImGui.InputInt("Index", ref index);
            if(ImGui.Button("Open context menu"))
            {
                if(TryGetAddonByName<AtkUnitBase>("_CharaSelectListMenu", out var addon) && IsAddonReady(addon))
                {
                    Callback.Fire(addon, false, (int)17, (int)1, (int)index);
                }
            }
            ImGuiEx.TextWrapped($"Names: {Utils.GetCharacterNames().Print()}");
        }
        if(ImGui.CollapsingHeader("Throttle"))
        {
            EzThrottler.ImGuiPrintDebugInfo();
            FrameThrottler.ImGuiPrintDebugInfo();
        }
        if(Svc.Targets.Target != null && Player.Available)
        {
            ImGuiEx.Text($"v.dist: {Svc.Targets.Target.Position.Y - Player.Object.Position.Y}");
            ImGuiEx.Text($"DTT3D: {Vector3.Distance(Svc.Targets.Target.Position, Player.Object.Position)}");
        }
    }

    private static void UnlockAllWorlds()
    {
        if(TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
        {
            var list = addon->UldManager.NodeList[6]->GetAsAtkComponentNode();
            for(var i = 3; i < 3 + 8; i++)
            {
                addon->AtkValues[160 + (i - 3) * 8].Int = 0;
                var t = list->Component->UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList[8]->GetAsAtkTextNode();
                if(t->Alpha_2 != 255)
                {
                    t->Alpha_2 = 255;
                }
            }
        }
    }

    private static void Editor()
    {
        var bsize = ImGuiHelpers.GetButtonSize("A") with { X = 280 };
        if(ImGui.Button("Save"))
        {
            ImGui.SetClipboardText(JsonConvert.SerializeObject(S.Data.DataStore.StaticData));
            S.Data.DataStore.StaticData.SaveConfiguration(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, S.Data.DataStore.FileName));
        }
        foreach(var x in S.Data.DataStore.Aetherytes)
        {
            ImGui.Separator();
            if(ImGui.Button($"{x.Key.Name}", bsize))
            {
                DebugAetheryte = x.Key;
            }
            {
                {
                    ImGui.SameLine();
                    if(!S.Data.DataStore.StaticData.SortOrder.ContainsKey(x.Key.ID)) S.Data.DataStore.StaticData.SortOrder[x.Key.ID] = 0;
                    var d = (int)S.Data.DataStore.StaticData.SortOrder[x.Key.ID];
                    ImGui.SetNextItemWidth(100f.Scale());
                    if(ImGui.InputInt($"##{x.Key.Name}{x.Key.ID}sort", ref d))
                    {
                        S.Data.DataStore.StaticData.SortOrder[x.Key.ID] = (uint)d;
                    }
                }
                if(ImGui.GetIO().KeyCtrl)
                {
                    ImGui.SameLine();
                    ImGuiEx.Text($"{x.Key.Position}");
                }
                if(Svc.Targets.Target != null)
                {
                    ImGui.SameLine();
                    if(ImGui.Button("Pos##" + x.Key.ID))
                    {
                        S.Data.DataStore.StaticData.CustomPositions[x.Key.ID] = Svc.Targets.Target.Position;
                        DuoLog.Information($"Written {Svc.Targets.Target.Position} for {x.Key.ID}");
                    }
                }
            }
            foreach(var l in x.Value)
            {
                if(ImGui.Button($"    {l.Name}", bsize)) DebugAetheryte = l;
                {
                    {
                        ImGui.SameLine();
                        if(!S.Data.DataStore.StaticData.SortOrder.ContainsKey(l.ID)) S.Data.DataStore.StaticData.SortOrder[l.ID] = 0;
                        var d = (int)S.Data.DataStore.StaticData.SortOrder[l.ID];
                        ImGui.SetNextItemWidth(100f.Scale());
                        if(ImGui.InputInt($"##{l.Name}{l.ID}sort", ref d))
                        {
                            S.Data.DataStore.StaticData.SortOrder[l.ID] = (uint)d;
                        }
                    }
                    if(ImGui.GetIO().KeyCtrl)
                    {
                        ImGui.SameLine();
                        ImGuiEx.Text($"{l.Position}");
                    }
                    if(Svc.Targets.Target != null)
                    {
                        ImGui.SameLine();
                        if(ImGui.Button("Pos##" + l.ID))
                        {
                            S.Data.DataStore.StaticData.CustomPositions[l.ID] = Svc.Targets.Target.Position;
                            DuoLog.Information($"Written {Svc.Targets.Target.Position} for {l.ID}");
                        }
                    }
                }
            }
        }
        ImGuiEx.Text(Utils.GetAvailableAethernetDestinations().Join("\n"));
        if(ImGui.Button($"null")) DebugAetheryte = null;
    }
}
