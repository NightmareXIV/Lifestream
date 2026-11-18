using BadgeOverlay;
using Dalamud.Interface.ManagedFontAtlas;
using ECommons.ExcelServices;
using ECommons.Interop;
using Lifestream.GUI;
using Lifestream.Systems.Residential;
using Lifestream.Tasks;
using Lifestream.Tasks.CosmicExploration;
using Lifestream.Tasks.CrossWorld;
using Lifestream.Tasks.SameWorld;
using Lumina.Excel.Sheets;
using NightmareUI.Censoring;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lifestream.Services;

public class BadgeOverlayManager : IDisposable
{
    private OverlayManager Overlay;
    private IFontHandle Font;
    private IFontHandle BadgeFont;
    private IFontHandle IconFont;
    private BadgeBar PrimaryBar;
    private BadgeDefinition LoginBadge;
    private BadgeDefinition InstanceBadge;
    private BadgeDefinition WorldBadge;
    private BadgeDefinition AethernetBadge;
    private BadgeDefinition WardBadge;
    private BadgeDefinition CosmicPlanetBadge;
    public BadgeOverlayManager()
    {
        BadgeFont = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(
            Svc.PluginInterface.UiBuilder.DefaultFontSpec.SizePx * C.BadgeFontSize)));
        Font = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(
            Svc.PluginInterface.UiBuilder.DefaultFontSpec.SizePx * C.PopoutFontSize)));
        IconFont = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddFontAwesomeIconFont(
            new()
            {
                SizePx = Svc.PluginInterface.UiBuilder.DefaultFontSpec.SizePx * C.PopoutFontSize
            })));

        Overlay = new(C.OverlayOptions);
        Overlay.Options.PreDraw = PreDraw;

        PrimaryBar = new BadgeBar("MasterBar", C.BadgeBarStyle);
        PrimaryBar.Style.GetFont = () => BadgeFont;

        InstanceBadge = new BadgeDefinition()
        {
            Id = "instance",
            Text = "Instance",
            Trigger = C.BadgeTriggerMode,
            Popout = new PopoutDefinition()
            {
                GetFont = () => Font,
                MinWidth = C.OverlayMinWidth,
                Draw = ctx =>
                {
                    if(S.InstanceHandler.InstancesInitizliaed(out var maxInstances))
                    {
                        if(ImGuiEx.BeginDefaultTable("popoutTableDef", ["~Name"], false))
                        {
                            for(var i = 1; i <= Math.Min(maxInstances, 9); i++)
                            {
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                var name = $"Instance {TaskChangeInstance.InstanceNumbers[i]}";
                                var d = S.InstanceHandler.GetInstance() == i;
                                if(ImGuiEx.Selectable($"{name}", !d, alignment: new(0.5f)))
                                {
                                    TaskRemoveAfkStatus.Enqueue();
                                    TaskChangeInstance.Enqueue(i);
                                    ctx.CloseAll();
                                }
                            }
                            ImGui.EndTable();
                        }
                    }
                    else
                    {
                        ImGuiEx.Text($"""
                        Instances available, 
                        but not initialized.

                        To initialize instances, 
                        access aetheryte once.
                        """);
                    }
                }
            },
            OnShow = () =>
            {
                InstanceBadge.OpenByDefault = C.InstancePinned;
            }
        };

        WorldBadge = new BadgeDefinition()
        {
            Id = "world",
            Text = "World",
            Trigger = C.BadgeTriggerMode,
            Popout = new PopoutDefinition()
            {
                GetFont = () => Font,
                MinWidth = C.OverlayMinWidth,
                Draw = ctx =>
                {
                    Dictionary<byte, List<World>> dataCenters = [];
                    foreach(var x in S.Data.DataStore.Worlds)
                    {
                        var world = ExcelWorldHelper.Get(x); 
                        if(world != null && C.OtherDCShowMode == Data.OtherDCShowMode.Show || world.Value.Unknown0 == Player.Object.CurrentWorld.Value.Unknown0 || (C.OtherDCShowMode == Data.OtherDCShowMode.Shown_on_Shift && IsKeyPressed([LimitedKeys.LeftShiftKey, LimitedKeys.RightShiftKey])))
                        {
                            dataCenters.GetOrCreate(world.Value.Unknown0, () => []).Add(world.Value);
                        }
                    }
                    dataCenters.Values.Each(x => x.SortBy(z => z.Name.ToString()));
                    var dcKeysOrdered = dataCenters.Keys.OrderBy(x => x).ToArray();
                    if(ImGuiEx.BeginDefaultTable("WorldTable", [.. dcKeysOrdered.Select(x => x.ToString())], false, extraFlags:ImGuiTableFlags.SizingStretchSame))
                    {
                        
                        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                        foreach(var dc in dcKeysOrdered)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.TextCentered(C.BadgeBarStyle.UseCustomColors?C.BadgeBarStyle.HeaderTextColor.ToVector4(): ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled], WorldDCGroupType.GetRef(dc).ValueNullable?.Name.ToString());
                        }
                        for(int j = 0; j < dataCenters.Values.Max(x => x.Count); j++)
                        {
                            ImGui.TableNextRow();
                            for(int i = 0; i < dcKeysOrdered.Length; i++)
                            {
                                ImGui.TableNextColumn(); 
                                if(dataCenters[dcKeysOrdered[i]].TryGetValue(j, out var world))
                                {
                                    var worldName = world.Name.ToString();
                                    var isHomeWorld = worldName == Player.HomeWorld;
                                    var cWorld = Svc.ClientState.LocalPlayer?.CurrentWorld.ValueNullable?.Name.ToString();
                                    var d = worldName == cWorld || Utils.IsDisallowedToChangeWorld();
                                    if(ImGuiEx.Selectable($"{(isHomeWorld ? (Lang.Symbols.HomeWorld + " ") : "")}{worldName}", !d, alignment: new(0.5f)))
                                    {
                                        TaskRemoveAfkStatus.Enqueue();
                                        TaskChangeWorld.Enqueue(worldName);
                                        TaskDesktopNotification.Enqueue($"Arrived to {worldName}");
                                        if(C.WorldVisitTPToAethernet && !C.WorldVisitTPTarget.IsNullOrEmpty() && !C.WorldVisitTPOnlyCmd)
                                        {
                                            P.TaskManager.Enqueue(() => Player.Interactable);
                                            P.TaskManager.Enqueue(() => TaskTryTpToAethernetDestination.Enqueue(C.WorldVisitTPTarget));
                                        }
                                        ctx.CloseAll();
                                    }
                                }
                            }
                        }
                        ImGui.EndTable();
                    }

                }
            },
            OnShow = () =>
            {
                WorldBadge.OpenByDefault = C.WorldPinned;
            }
        };

        AethernetBadge = new BadgeDefinition()
        {
            Id = "aethernet",
            Text = "Aethernet",
            Trigger = C.BadgeTriggerMode,
            Popout = new PopoutDefinition()
            {
                GetFont = () => Font,
                MinWidth = C.OverlayMinWidth,
                Draw = DrawNormalAethernet,
            },
            OnShow = () =>
            {
                AethernetBadge.OpenByDefault = C.AethernetPinned;
            }
        };

        CosmicPlanetBadge = new BadgeDefinition()
        {
            Id = "cosmicplanet",
            Text = "Planet",
            Trigger = C.BadgeTriggerMode,
            Popout = new PopoutDefinition()
            {
                GetFont = () => Font,
                MinWidth = C.OverlayMinWidth,
                Draw = DrawCosmicBadge,
            },
            OnShow = () =>
            {
                CosmicPlanetBadge.OpenByDefault = C.CosmicPinned;
            }
        };

        WardBadge = new BadgeDefinition()
        {
            Id = "ward",
            Text = "Housing",
            Trigger = C.BadgeTriggerMode,
            Popout = new PopoutDefinition()
            {
                GetFont = () => Font,
                MinWidth = C.OverlayMinWidth,
                Draw = ctx =>
                {
                    if(ImGuiEx.BeginDefaultTable("hTable", ["1", "2", "3"], false, extraFlags: ImGuiTableFlags.SizingStretchSame))
                    {
                        for(var i = 1; i <= 30; i++)
                        {
                            ImGui.TableNextColumn();
                            if(ImGuiEx.Selectable($"  {i}  ##ward", alignment: new(0.5f)))
                            {
                                TaskRemoveAfkStatus.Enqueue();
                                TaskGoToResidentialDistrict.Enqueue(i);
                                ctx.CloseAll();
                            }
                        }
                        ImGui.EndTable();
                    }
                }
            },
            OnShow = () =>
            {
                WardBadge.OpenByDefault = C.WardPinned;
            }
        };

        LoginBadge = new BadgeDefinition
        {
            Id = "login",
            Text = "Lifestream",
            Trigger = C.BadgeTriggerMode,
            Popout = new PopoutDefinition
            {
                GetFont = () => Font,
                MinWidth = C.OverlayMinWidth,
                Draw = ctx =>
                {
                    Dictionary<ExcelWorldHelper.Region, Dictionary<WorldDCGroupType, Dictionary<World, List<string>>>> worldNames = [];
                    foreach(var x in C.CharaMap)
                    {
                        var split = x.Value.Split("@");
                        if(split.Length != 2)
                        {
                            continue;
                        }

                        var world = ExcelWorldHelper.Get(split[1]);
                        if(world == null)
                        {
                            continue;
                        }

                        var region = world.Value.GetRegion();
                        var dc = world.Value.DataCenter.Value;
                        worldNames
                        .GetOrCreate(region, () => [])
                        .GetOrCreate(dc, () => [])
                        .GetOrCreate(world.Value, () => [])
                        .Add(split[0]);
                    }
                    foreach(var regionDefinition in worldNames.OrderBy(v => v.Key))
                    {
                        foreach(var datacenter in regionDefinition.Value.OrderBy(v => v.Key.Name.ToString()))
                        {
                            var worlds = datacenter.Value.OrderBy(v => v.Key.Name.ToString());
                            ctx.Selectable($" {regionDefinition.Key} - {datacenter.Key.Name}", () => new PopoutDefinition()
                            {
                                GetFont = () => Font,
                                Draw = child =>
                                {
                                    foreach(var world in worlds)
                                    {
                                        var charas = world.Value;
                                        child.Selectable($" - {world.Key.Name} -", () => new PopoutDefinition()
                                        {
                                            GetFont = () => Font,
                                            MinWidth = C.OverlayMinWidth,
                                            Draw = child =>
                                            {
                                                if(ImGuiEx.BeginDefaultTable("ctable", ["~Chara name", "Control"], false))
                                                {
                                                    foreach(var chara in charas)
                                                    {
                                                        ImGui.PushID(chara);
                                                        ImGui.TableNextRow();
                                                        ImGui.TableNextColumn();
                                                        if(ImGui.Selectable($" {Censor.Character(chara)} "))
                                                        {
                                                            Svc.Commands.ProcessCommand($"/li {chara}@{world.Key.Name}");
                                                            ctx.CloseAll();
                                                        }
                                                        ImGui.TableNextColumn();
                                                        using(var iconFont = IconFont?.Push())
                                                        {
                                                            if(ImGui.Selectable(FontAwesomeIcon.Plane.ToIconString()))
                                                            {
                                                                Svc.Commands.ProcessCommand($"/li w {chara}@{world.Key.Name}");
                                                                ctx.CloseAll();
                                                            }
                                                        }
                                                        ImGui.PopID();
                                                    }
                                                    ImGui.EndTable();
                                                }
                                            }
                                        });
                                    }
                                },
                            });

                        }
                    }
                },
            },
        };

        PrimaryBar.Badges.Add(LoginBadge);
        PrimaryBar.Badges.Add(InstanceBadge);
        PrimaryBar.Badges.Add(WorldBadge);
        PrimaryBar.Badges.Add(AethernetBadge);
        PrimaryBar.Badges.Add(CosmicPlanetBadge);
        PrimaryBar.Badges.Add(WardBadge);

        Overlay.AddBar(PrimaryBar);

        Svc.PluginInterface.UiBuilder.Draw += Overlay.Draw;
    }

    private void DrawCosmicBadge(PopoutContext context)
    {
        if(ImGuiEx.BeginDefaultTable("hTable", ["1"], false, extraFlags: ImGuiTableFlags.SizingStretchSame))
        {
            foreach(var x in Enum.GetValues<CosmicPlanet>())
            {
                ImGui.TableNextColumn();
                if(ImGuiEx.Selectable($"{TaskTravelToPlanet.Names.SafeSelect(x)}##planet", alignment: new(0.5f), enabled:!x.IsHere()))
                {
                    TaskRemoveAfkStatus.Enqueue();
                    TaskTravelToPlanet.Enqueue(x);
                    context.CloseAll();
                }
            }
            ImGui.EndTable();
        }
    }

    private void DrawNormalAethernet(PopoutContext context)
    {

        if(S.Data.ResidentialAethernet.IsInResidentialZone())
        {
            if(ImGuiEx.BeginDefaultTable("popoutTableDef", ["Name", "Name2"], false, extraFlags: ImGuiTableFlags.SizingStretchSame))
            {
                var zinfo = S.Data.ResidentialAethernet.ZoneInfo[P.Territory];
                var normal = zinfo.Aetherytes.Where(x => !x.IsSubdivision).OrderByDescending(x => C.Favorites.Contains(x.ID)).ToArray();
                var subdiv = zinfo.Aetherytes.Where(x => x.IsSubdivision).OrderByDescending(x => C.Favorites.Contains(x.ID)).ToArray();
                for(int i = 0; i < Math.Max(normal.Length, subdiv.Length); i++)
                {
                    var normalItem = normal.SafeSelectNullable(i);
                    var subItem = subdiv.SafeSelectNullable(i);

                    var showNormal = (normalItem != null && !C.Hidden.Contains(normalItem.Value.ID));
                    var showSub = (subItem != null && !C.Hidden.Contains(subItem.Value.ID));

                    if(showNormal || showSub)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if(showNormal)
                        {
                            draw(normalItem.Value);
                        }
                        ImGui.TableNextColumn();
                        if(showSub)
                        {
                            draw(subItem.Value);
                        }
                        static void draw(ResidentialAetheryte x)
                        {
                            var name = (C.Favorites.Contains(x.ID) ? Lang.Symbols.HighQuality + " " : "") + (C.Renames.TryGetValue(x.ID, out var value) ? value : x.Name);
                            var d = S.Data.ResidentialAethernet.ActiveAetheryte == x;
                            if(ImGuiEx.Selectable($"{name}", !d, alignment: new(0.5f)))
                            {
                                TaskRemoveAfkStatus.Enqueue();
                                TaskAethernetTeleport.Enqueue(x.Name);
                            }
                            S.Gui.Overlay.Popup(x);
                        }
                    }
                }
                ImGui.EndTable();
            }
        }
        else if(S.Data.CustomAethernet.ZoneInfo.TryGetValue(P.Territory, out var zinfo))
        {
            if(ImGuiEx.BeginDefaultTable("popoutTableDef", ["~Name"], false))
            {
                foreach(var x in zinfo.Aetherytes.OrderByDescending(x => C.Favorites.Contains(x.ID)))
                {
                    if(!C.Hidden.Contains(x.ID))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var name = (C.Favorites.Contains(x.ID) ? Lang.Symbols.HighQuality + " " : "") + (C.Renames.TryGetValue(x.ID, out var value) ? value : x.Name);
                        var d = S.Data.CustomAethernet.ActiveAetheryte == x;
                        if(ImGuiEx.Selectable($"{name}", !d, alignment: new(0.5f)))
                        {
                            TaskRemoveAfkStatus.Enqueue();
                            TaskAethernetTeleport.Enqueue(x.Name);
                        }
                        S.Gui.Overlay.Popup(x);
                    }
                }
                ImGui.EndTable();
            }
        }
        else if(P.ActiveAetheryte != null)
        {
            if(ImGuiEx.BeginDefaultTable("popoutTableDef", ["~Name"], false))
            {
                var master = Utils.GetMaster();
                if(!C.Hidden.Contains(master.ID))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var name = (C.Favorites.Contains(master.ID) ? Lang.Symbols.HighQuality + " " : "") + (C.Renames.TryGetValue(master.ID, out var value) ? value : master.Name);
                    var md = P.ActiveAetheryte == master;
                    if(ImGuiEx.Selectable($"{name}", !md, alignment: new(0.5f)))
                    {
                        TaskRemoveAfkStatus.Enqueue();
                        TaskAethernetTeleport.Enqueue(master);
                        context.CloseAll();
                    }
                    S.Gui.Overlay.Popup(master);
                }

                foreach(var x in S.Data.DataStore.Aetherytes[master])
                {
                    if(!C.Hidden.Contains(x.ID))
                    {
                        ImGui.TableNextColumn();
                        var name = (C.Favorites.Contains(x.ID) ? Lang.Symbols.HighQuality + " " : "") + (C.Renames.TryGetValue(x.ID, out var value) ? value : x.Name);
                        var d = P.ActiveAetheryte == x;
                        if(ImGuiEx.Selectable($"{name}", !d, alignment: new(0.5f)))
                        {
                            TaskRemoveAfkStatus.Enqueue();
                            TaskAethernetTeleport.Enqueue(x);
                            context.CloseAll();
                        }
                        S.Gui.Overlay.Popup(x);
                    }
                }

                if(P.ActiveAetheryte?.ID == 70 && C.Firmament)
                {
                    ImGui.TableNextColumn();
                    var name = "Firmament";
                    if(ImGuiEx.Selectable($"{name}", alignment: new(0.5f)))
                    {
                        TaskRemoveAfkStatus.Enqueue();
                        TaskFirmanentTeleport.Enqueue();
                        context.CloseAll();
                    }
                }
                ImGui.EndTable();
            }
        }
    }

    private void PreDraw()
    {
        var busy = P.TaskManager.IsBusy || IsOccupied();
        PrimaryBar.Visible = C.Enable;
        LoginBadge.Visible = false;// !Svc.ClientState.IsLoggedIn && Utils.CanAutoLogin();
        InstanceBadge.Visible = !busy && (S.InstanceHandler.GetInstance() != 0 && C.ShowInstanceSwitcherBOM && (P.ActiveAetheryte == null || P.ActiveAetheryte.Value.IsAetheryte)) && S.InstanceHandler.CanChangeInstance();
        AethernetBadge.Visible = !busy && Utils.CanUseAetheryte() != Enums.AetheryteUseState.None && C.ShowAethernetBOM;
        WorldBadge.Visible = !busy && P.ActiveAetheryte != null && P.ActiveAetheryte.Value.IsWorldChangeAetheryte() && C.ShowWorldVisitBOM;
        WardBadge.Visible = !busy && P.ActiveAetheryte != null && C.ShowWardsBOM && Utils.HousingAethernet.Contains(P.Territory) && P.ActiveAetheryte.Value.IsResidentialAetheryte();
        CosmicPlanetBadge.Visible = !busy && C.ShowCosmicBOM && TaskTravelToPlanet.GetNearbySwitchingNpc() != null;
    }

    public void Dispose()
    {
        Font?.Dispose();
        Font = null;
        IconFont?.Dispose();
        IconFont = null;
        BadgeFont?.Dispose();
        BadgeFont = null;
        Svc.PluginInterface.UiBuilder.Draw -= Overlay.Draw;
        Overlay.CloseAll();
    }
}
